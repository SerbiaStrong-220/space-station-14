import * as core from '@actions/core';
import * as github from '@actions/github';
import * as discord from 'discord.js';
import * as fs from 'fs';
import * as url from 'url';
import * as util from 'util';
import * as https from 'https';
import * as path from 'path';
import * as crypto from 'crypto';

//#region  Inputs
// required inputs
const WEBHOOK_TOKEN = process.env.WEBHOOK_TOKEN;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const PULL_URL = process.env.PULL_URL;

let missing_inputs = [];
if (WEBHOOK_TOKEN === undefined) missing_inputs.push(`WEBHOOK_TOKEN`);
if (GITHUB_TOKEN === undefined) missing_inputs.push(`GITHUB_TOKEN`);
if (PULL_URL === undefined) missing_inputs.push(`PULL_URL`);

if (missing_inputs.length > 0){
    core.setFailed(`Missing 1 or more required inputs: ${missing_inputs.join(`, `)}`)
    exit(1);
}

// optional inputs
const WEBHOOK_USERNAME = process.env.WEBHOOK_USERNAME;
const WEBHOOK_AVATAR_URL = process.env.WEBHOOK_AVATAR_URL;
//#endregion

const REPLACE_DATA = new Map([
    ["add:", ":newspaper: "],
    ["remove:", ":scissors: "],
    ["tweak:", ":gear: "],
    ["fix:", ":tools: "]
]);

const MEDIA_TYPES = Object.freeze({
    IMAGE: 'IMAGE',
    VIDEO: 'VIDEO',
});

const ATTACHMENT_COUNT_LIMIT = 10; // docs: https://discord.com/developers/docs/resources/message#create-message
const ATTACHMENT_SIZE_LIMIT = 10 * 1024 * 1024; // docs: https://discord.com/developers/docs/resources/message#create-message

const DIRECTORY_NAME = path.dirname(url.fileURLToPath(import.meta.url));
const ATTACHMENT_SAVE_PATH = path.join(DIRECTORY_NAME, 'temp');

const LOG_SEPARATOR = '========================================';

class MediaData{
    /**
     * @param {string} name
     * @param {string} path
     * @param {string} type
     * @param {number} size
     */
    constructor(name, path, type, size){
        this.name = name;
        this.path = path;
        this.type = type;
        this.size = size;
    }
}

try {
    await run();
    exit(0);
} catch (e) {
    core.setFailed(`${e.message}\n${e.stack}`);
    exit(1);
}

async function run(){
    let [webhookId, webhookToken] = WEBHOOK_TOKEN.split('/');
    const webhook_client = new discord.WebhookClient({ id: webhookId, token: webhookToken });
    const git_client = github.getOctokit(GITHUB_TOKEN);

    core.info(`Attempt to send a GET-request to ${PULL_URL}`);
    const pull_request = await git_client.request(`GET ${PULL_URL}`);
    const html_url = pull_request.data.html_url;

    let text = pull_request.data.body;
    if (text === null){
        core.warning("Pull request body is empty, nothing to extract");
        return;
    }

    core.info('');
    core.info('Input message:');
    core.info(LOG_SEPARATOR);
    core.info(text.trim());
    core.info(LOG_SEPARATOR);
    core.info('');

    text = removeCLIgnore(text);
    text = removeGitComments(text);

    let outputCLData = getChangelogData(text, pull_request.data.user.login);
    if (outputCLData === null) return;

    core.info('');
    core.info('Output message:');
    core.info(LOG_SEPARATOR);
    core.info(outputCLData.authors);
    core.info(outputCLData.infos);
    core.info(LOG_SEPARATOR);
    core.info('');
    
    let title = `#${pull_request.data.number}: ${pull_request.data.title}`;
    let mainEmbed = new discord.EmbedBuilder()
        .setColor(0x3CB371)
        .setTitle(title)
        .setURL(html_url)
        .setDescription(`**${outputCLData.authors}**\n${outputCLData.infos}`);


    // Trying to download all media from the PR
    let mediaMap = await getMedia(text);
    let embeds = [mainEmbed];
    let attachments = [];


    // Attach images
    let images = mediaMap.get(MEDIA_TYPES.IMAGE);
    if (images !== undefined){
        if (images.length > ATTACHMENT_COUNT_LIMIT){
            core.warning(`More than ${ATTACHMENT_COUNT_LIMIT} images found, only the first ${ATTACHMENT_COUNT_LIMIT} will be sent`);
        }

        let accumulatedSize = 0;
        for (let i = 0; i < images.length; i++){
            if (i >= ATTACHMENT_COUNT_LIMIT) break;

            let image = images[i];
            if (image === undefined) continue;

            accumulatedSize += image.size;
            if (accumulatedSize > ATTACHMENT_SIZE_LIMIT){
                core.warning(`${image.name} will exceed the message size by up to ${accumulatedSize} bytes, which exceeds the limit of ${ATTACHMENT_SIZE_LIMIT} bytes. Skip it`);
                accumulatedSize -= image.size;
                continue;
            }

            attachments.push(new discord.AttachmentBuilder(image.path, {name: image.name}));
            if (i == 0){
                mainEmbed.setImage(`attachment://${image.name}`);
            }
            else{
                let newEmbed = new discord.EmbedBuilder()
                    .setURL(html_url)
                    .setImage(`attachment://${image.name}`);
                embeds.push(newEmbed);
            }
        }
    }

    let options = {embeds: embeds};
    if (WEBHOOK_USERNAME !== undefined){
        options = {
            ...options,
            username: WEBHOOK_USERNAME
        }
    }

    if (WEBHOOK_AVATAR_URL !== undefined){
        options = {
            ...options,
            avatarURL: WEBHOOK_AVATAR_URL
        }
    }

    if (attachments.length > 0){
        options = {
            ...options,
            files: attachments
        }
    }

    await webhook_client.send(options);


    // Attach videos
    // Videos will be sent as a separate message
    let videos = mediaMap.get(MEDIA_TYPES.VIDEO);
    if (videos !== undefined){
        if (videos.length > ATTACHMENT_COUNT_LIMIT){
            core.warning(`More than ${ATTACHMENT_COUNT_LIMIT} videos found, only the first ${ATTACHMENT_COUNT_LIMIT} will be sent`);
        }

        attachments = [];
        let accumulatedSize = 0;
        for (let i = 0; i < videos.length; i++){
            if (i >= ATTACHMENT_COUNT_LIMIT) break;

            let video = videos[i];
            if (video === undefined) continue;

            accumulatedSize += video.size;
            if (accumulatedSize > ATTACHMENT_SIZE_LIMIT){
                core.warning(`${video.name} will exceed the message size by up to ${accumulatedSize} bytes, which exceeds the limit of ${ATTACHMENT_SIZE_LIMIT} bytes! Skiping it`);
                accumulatedSize -= video.size;
                continue;
            }

            attachments.push(new discord.AttachmentBuilder(video.path, {name: video.name}));
        }

        await webhook_client.send({
            username: WEBHOOK_USERNAME,
            avatarURL: WEBHOOK_AVATAR_URL,
            files: attachments
        });
    }
}

/**
 * @param {string} text
 * @param {string} default_author
 * @returns {{ authors: string; infos: string; } | null}
 */
function getChangelogData(text, default_author = `Unknown`){
    let clText = extractCLText(text);
    if (clText === null){
        core.warning(`Doesn't found any changelog`);
        return null;
    }

    core.info(`Found changelog`);

    let infoArray = extractInfoLines(clText);
    if (infoArray === null || infoArray.length <= 0){
        core.warning(`Doesn't found any info string`)
        return null;
    }

    core.info(`Found ${infoArray.length} info strings`)
    for (let i = 0; i < infoArray.length; i++){
        let curInfo = infoArray[i];

        if (typeof curInfo !== 'string' ||
            curInfo === null ||
            curInfo === ''){
            infoArray[i] = '';
            continue;
        }

        const dash_regex = /\s*-\s*(?=\w+:)/g;

        curInfo = curInfo.replaceAll(dash_regex, "");
        for (let [key, value] of REPLACE_DATA){
            curInfo = curInfo.replaceAll(key, value);
        }

        infoArray[i] = curInfo;
    }

    infoArray = infoArray.filter(x => x !== '');
    let infos = infoArray.join('\n');

    if (infos === ''){
        core.error(`Failed to generate the final info string`);
        return null;
    }

    let authors = "";
    let authorsArray = extractAuthors(clText);
    if (authorsArray === null || authorsArray.length <= 0){
        core.info(`Doesn't found authors in the input text, the user's login will be used instead`)
        authors = `Автор: ${default_author}`;
    } else if (authorsArray.length === 1){
        core.info(`Found 1 author in the input text`)
        authors = `Автор: ${authorsArray[0]}`;
    } else{
        core.info(`Found ${authorsArray.length} authors in the input text`)
        authors = `Авторы:`
        for (let i = 0; i < authorsArray.length; i++){
            if (i !== authorsArray.length - 1){
                authors += ` ${authorsArray[i]},`
            } else{
                authors += ` ${authorsArray[i]}`
            }
        }
    }

    return {authors, infos}

    /**
     * @param {string} text
     * @returns {string | null}
     */
    function extractCLText(text){
        const cl_regex = /(?<=\s*):cl:|(?<=\s*)\uD83C\uDD91/gm;

        let m = cl_regex.exec(text);
        if (m === null) return null;

        return text.substring(m.index);
    }

    /**
     * @param {string} text
     * @returns {string[]}
     */
    function extractInfoLines(text){
        const info_line_regex = /\s*-\s*\w+:.*$/gm;

        let infoLinesArray = [];
        let result;
        while((result = info_line_regex.exec(text)) != null){
            infoLinesArray.push(result[0]);
        }

        return infoLinesArray;
    }

    /**
     * @param {string} text
     * @returns {string[] | null}
     */
    function extractAuthors(text){
        const authors_line_regex = /(?<=:cl:).*|(?<=^\uD83C\uDD91)/g;

        let authorsLine = authors_line_regex.exec(text);
        if (authorsLine === null) return null;

        let authorString = authorsLine[0].trim();
        if (authorString === "") return null;

        let authorsArray = authorString.split(',');
        authorsArray = authorsArray.map(a => a.trim()).filter(a => a !== "");
        return authorsArray;
    }
}

/**
 * @param {string} text
 * @returns {Promise<Map<string, MediaData[]>>}
 */
async function getMedia(text){
    core.info(`\n`);
    const url_regex = /(http|https):\/\/[^")\]\s]+/gm;

    let mediaMap = new Map();
    let i = 0;
    let result;
    while((result = url_regex.exec(text)) != null){
        let mediaUrl = result[0];
        core.info(`Try downloading media from ${mediaUrl}`);
        let media = await downloadMedia(mediaUrl);
        if (media === null) continue;

        if (mediaMap.has(media.type)){
            mediaMap.get(media.type).push(media);
        }
        else{
            mediaMap.set(media.type, [media]);
        }
        i++
    }

    core.info(`\n`);
    return mediaMap;

    /**
     * @param {string} mediaUrl
     * @returns {Promise<MediaData | null>}
     */
    async function downloadMedia(mediaUrl){
        const write_file_async = fs.promises.writeFile;
        if (!fs.existsSync(ATTACHMENT_SAVE_PATH)){
            fs.mkdirSync(ATTACHMENT_SAVE_PATH);
        }

        try {
            const {extension, mediaType, buffer} = await sendRequest(mediaUrl);
            let size = buffer.length;

            core.info(`Downloaded file size: ${size}`);
            if (size > ATTACHMENT_SIZE_LIMIT){
                core.warning(`File size in "${mediaUrl}" is more than ${ATTACHMENT_SIZE_LIMIT} bytes!`);
                return null;
            }

            let fileName = generateFileName(extension);
            let savePath = path.join(ATTACHMENT_SAVE_PATH, fileName);
            await write_file_async(savePath, buffer);

            core.info(`Download successful`);
            return new MediaData(fileName, savePath, mediaType, size);
        } catch (e){
            core.warning(`Download failed by error: ${e.message}`);
            return null;
        }
    }

    /**
     * @param {string} fileUrl
     * @returns {Promise<{ extension: string; mediaType: string; buffer: Buffer<ArrayBuffer>; }>}
     */
    function sendRequest(fileUrl){
        return new Promise((resolve, reject) => {
            const normalized_url = new URL(fileUrl);

            const options = {
                hostname: normalized_url.hostname,
                path: normalized_url.pathname + normalized_url.search,
                method: 'GET',
                headers: {
                    'User-Agent': 'Mozilla/5.0', // avoids some GitHub 403s
                }
            };

            const req = https.request(options, (res) => {
                // Handle redirects
                if (res.statusCode !== undefined && res.statusCode >= 300 && res.statusCode < 400 && res.headers.location !== undefined) {
                    return resolve(sendRequest(res.headers.location));
                }

                let extension;
                let mediaType;
                let contentType = res.headers['content-type'];

                if (contentType === undefined){
                    return reject(`Unable to determine a media type`);
                }

                if (contentType.endsWith('/gif')){
                    extension = 'gif';
                    mediaType = MEDIA_TYPES.VIDEO;
                }
                else if (contentType.startsWith('image/')){
                    extension = contentType.split('/')[1].split(';')[0];
                    mediaType = MEDIA_TYPES.IMAGE;
                }
                else if (contentType.startsWith('video/')){
                    extension = contentType.split('/')[1].split(';')[0];
                    mediaType = MEDIA_TYPES.VIDEO;
                }

                if (extension === undefined || extension === null){
                    return reject(`Content-Type "${contentType}" is not supported!`);
                }

                let data = [];
                res.on('data', chunk => data.push(chunk));
                res.on('end', () => {
                    resolve({extension: extension, mediaType: mediaType, buffer: Buffer.concat(data)});
                });
            });

            req.on('error', reject);
            req.end();
        });
    }

    function generateFileName(extension){
        const random_part = crypto.randomBytes(4).toString('hex');
        const timestamp = Date.now();
        return `${random_part}_${timestamp}.${extension}`;
    }
}

/**
 * Removes text marked with \<!--CLIgnore--\> tag
 */
function removeCLIgnore(text){
    const tag = `CLIgnore`;
    const cl_ignore_regex = new RegExp(`<!--${tag}-->[\\s\\S]*?<!--\/${tag}-->`, `gm`);

    return text.replaceAll(cl_ignore_regex, '').trim();
}

/**
 * Removes text marked as comments
 */
function removeGitComments(text){
    const comment_regex = /<!--[\s\S]*?-->|<!--[\s\S]*/gm;

    return text.replaceAll(comment_regex, '').trim();
}

/**
 * @param {string | number | null | undefined} code
 * @returns {never}
 */
function exit(code = undefined){
    if (fs.existsSync(ATTACHMENT_SAVE_PATH)){
        fs.rmSync(ATTACHMENT_SAVE_PATH, {recursive: true});
    }

    process.exit(code);
}
