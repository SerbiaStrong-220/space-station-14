## Kontra

# Shown at the end of a round of kontrrazvedchik
kontra-round-end-result = {$kontraCount ->
    [one] There was one kontra.
    *[other] There were {$kontraCount} kontras.
}

kontra-round-end-codewords = The codewords were: [color=White]{$codewords}[/color]

# Shown at the end of a round of kontra
kontra-user-was-a-kontra = [color=gray]{$user}[/color] was a kontrrazvedchik.
kontra-user-was-a-kontra-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a kontrrazvedchik.
kontra-was-a-kontra-named = [color=White]{$name}[/color] was a kontrrazvedchik.

kontra-user-was-a-kontra-with-objectives = [color=gray]{$user}[/color] was a kontra who had the following objectives:
kontra-user-was-a-kontra-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) was a kontra who had the following objectives:
kontra-was-a-kontra-with-objectives-named = [color=White]{$name}[/color] was a kontra who had the following objectives:

preset-kontra-objective-issuer-syndicate = [color=#87cefa]The Syndicate[/color]

# Shown at the end of a round of kontra
kontra-objective-condition-success = {$condition} | [color={$markupColor}]Success![/color]

# Shown at the end of a round of kontra
kontra-objective-condition-fail = {$condition} | [color={$markupColor}]Failure![/color] ({$progress}%)

kontra-title = kontra
kontra-description = There are kontras among us...
kontra-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start kontra.
kontra-no-one-ready = No players readied up! Can't start kontra.

## kontraDeathMatch
kontra-death-match-title = kontra Deathmatch
kontra-death-match-description = Everyone's a kontra. Everyone wants each other dead.
kontra-death-match-station-is-too-unsafe-announcement = The station is too unsafe to continue. You have one minute.
kontra-death-match-end-round-description-first-line = The PDAs recovered afterwards...
kontra-death-match-end-round-description-entry = {$originalName}'s PDA, with {$tcBalance} TC

## kontraRole

# kontraRole
kontra-role-greeting =
    You are a syndicate agent.
    Your objectives and codewords are listed in the character menu.
    Use the uplink loaded into your PDA to buy the tools you'll need for this mission.
    Death to Nanotrasen!
kontra-role-codewords =
    The codewords are:
    {$codewords}.
    Codewords can be used in regular conversation to identify yourself discretely to other syndicate agents.
    Listen for them, and keep them secret.
kontra-role-uplink-code =
    Set your ringtone to the notes {$code} to lock or unlock your uplink.
    Remember to lock it after, or the stations crew will easily open it too!

# don't need all the flavour text for character menu
kontra-role-codewords-short =
    The codewords are:
    {$codewords}.
kontra-role-uplink-code-short = Your uplink code is {$code}.
