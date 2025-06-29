from flask import Flask, json
from flask import request
from tts_processor import tts_creator

api = Flask(__name__)
primary_speaker = 'aidar'

host = "127.0.0.1"
port = 5000
tts_module = tts_creator()

#Get request, consume text, make tts, build response, return to sender.
@api.route('/tts/', methods=['GET'])
def process_tts():
   text = request.args.get('text')
   original_speaker = request.args.get('speaker')
   extension = request.args.get('ext')
   print(f'Got request with text "{text}" and speaker: "{original_speaker}"') #Strictly debugging thing, uncomment if uncomfortable.
   speaker = primary_speaker
   if extension == 'ogg':
      payload = tts_module.make_ogg(text=text, speaker=speaker, sample_rate=24000)
   else:
      payload = tts_module.make_wav(text=text, speaker=speaker, sample_rate=24000)
   return payload

if __name__ == '__main__':
    #Note: if you don't change host and port, default setting to import to sensitive.dm will be "http://127.0.0.1:5000/tts/"
    print(f'Server is starting up. TTS URL: "http://{host}:{port}/tts/"')
    api.run(host=host, port=port)

