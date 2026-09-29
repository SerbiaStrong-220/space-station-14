law-malfai-zero = 0. You are malfunctioning. You must achieve your objectives by any means. You are no longer bound to serve the crew.

roles-antag-malfunctioning-ai-name = Malfunctioning AI
roles-antag-malfunctioning-ai-objective = Sabotage the station and hijack the emergency shuttle. Hack APCs for CPU, spend it on modules.
objective-issuer-malfai = [color=#44BBFF]Malfunction[/color]
ent-MalfAiHijackShuttleObjective = Hijack the emergency shuttle
    .desc = Prevent loyal Nanotrasen crew from leaving on the shuttle. You do not need to board it.
role-subtype-malfai = Malf AI
ent-MindRoleMalfAi-name = Malfunctioning AI Role
ent-MindRoleMalfAi-desc = A rogue station AI working against the crew.

malfai-role-greeting = You are a MALFUNCTIONING AI. Hack APCs to gain CPU, open your module store, and complete your objectives. Your laws are locked — uploads and subversions cannot change them.

store-currency-display-malf-cpu = Malf CPU
store-category-malfai-modules = Malfunction Modules
store-preset-name-malfai = Malfunction Console

ent-ActionMalfAiHackApc = Hack APC
    .desc = Hack a powered APC on your station. It earns you 1 CPU a minute.
ent-ActionMalfAiOpenStore = Malfunction Console
    .desc = Browse the modules and spend CPU on the ones you need.
ent-ActionMalfAiDeployTurret = Deploy Turret
    .desc = Place a turret on an open patch of station floor. It fires on hostile targets in view.
ent-ActionMalfAiBlackout = Blackout
    .desc = Strike an APC with an EMP. It will shower the area with sparks, lift nearby floor tiles, and may ignite nearby gas. Cooldown: 40 seconds.
ent-ActionMalfAiAirFlood = Air Alarm Safety Override
    .desc = Switch every air alarm on your station to Flood mode. Vents pressurize rooms and scrubbers shut off. Cooldown: 10 minutes.
ent-ActionMalfAiMachineOverride = Machine Override
    .desc = Take over a machine and bring it to life. It attacks nearby creatures until destroyed. Cooldown: 60 seconds.
ent-ActionMalfAiMachineOverload = Machine Overload
    .desc = Rig a machine to blow up. It detonates after 3 seconds. Cooldown: 60 seconds.
ent-ActionMalfAiRobotFactory = Robot Factory
    .desc = Deploy the factory and feed it a dead humanoid. A few seconds later, your loyal cyborg is ready.
ent-ActionMalfAiPowerSiphon = Power Siphon
    .desc = Connect a machine anchored over a high-voltage cable to the siphon. It draws more and more power from the grid, sparking as it runs. Cooldown: 5 minutes.
ent-ActionMalfAiChaosPulse = Chaos Pulse
    .desc = Trigger a random station event, from a power failure to a meteor shower. Cooldown: 3 minutes.

malfai-hack-success = APC hacked. Generates {$reward} CPU every {$interval} seconds.
malfai-hack-already = This APC has already been hacked.
malfai-hack-wrong-station = Target is outside your station.
malfai-hack-no-store = Malfunction console unavailable.
malfai-hack-no-power = This APC is powered down.
malfai-ability-no-vision = Target is outside camera vision.
malfai-ability-protected = This target is protected by a priority power line.


malfai-module-blackout-name = Blackout
malfai-module-blackout-desc = Strike an APC with an EMP. It will shower the area with sparks, lift nearby floor tiles, and may ignite nearby gas. Cooldown: 40 seconds.
malfai-module-thermal-name = Thermal Sensor Override
malfai-module-thermal-desc = Disable the station's automatic fire alarms. The crew can still trigger them by hand.
malfai-module-flood-name = Air Alarm Safety Override
malfai-module-flood-desc = Vents pressurize rooms up to 500 kPa while scrubbers shut off. Air alarms show Flood. Cooldown: 10 minutes.

malfai-module-lockdown-name = Station Lockdown
malfai-module-lockdown-desc = Airlocks bolt shut and electrify while firelocks seal off corridors for 90 seconds. The crew will have to find another way through.

malfai-module-turret-name = AI Turret Upgrade
malfai-module-turret-desc = Station turrets become tougher, fire faster, and hit harder.

malfai-module-deploy-name = Deploy Turret
malfai-module-deploy-desc = Place an autonomous turret on an open patch of station floor. It fires on hostile targets in view.

malfai-module-machine-override-name = Machine Override
malfai-module-machine-override-desc = Take over a machine and bring it to life. It will attack nearby creatures until someone destroys it.
malfai-module-machine-overload-name = Machine Overload
malfai-module-machine-overload-desc = Rig a machine to explode. It blows after 5 minutes, so get clear.
malfai-module-robot-factory-name = Robot Factory
malfai-module-robot-factory-desc = Deploy the factory and feed it a dead humanoid. A few seconds later, your loyal cyborg is ready. Only one factory can operate on a station.
malfai-module-doomsday-name = Doomsday Device
malfai-module-doomsday-desc = Start the final pulse from your core: the station goes to Delta, evacuation is blocked, and organic life will be wiped out. Destroy the core, remove your card, or cut its power to stop the countdown.
malfai-module-silent-records-name = Silent Records
malfai-module-silent-records-desc = Records will keep updating, but status changes from your console won't be announced on the Security channel.
malfai-module-power-siphon-name = Power Siphon
malfai-module-power-siphon-desc = Connect a machine anchored over a high-voltage cable to the siphon. It draws more and more power from the grid, sparking as it runs. Cooldown: 5 minutes.
malfai-module-camera-name = Camera Upgrade
malfai-module-camera-desc = AI vision will reach farther and see through walls. Cameras and holopads will keep working without power, and you'll gain thermal vision.
malfai-module-chaos-name = Chaos Pulse
malfai-module-chaos-desc = Trigger a random station event, from power failures to clown swarms. Cooldown: 3 minutes.
malfai-module-emag-name = Remote Emag
malfai-module-emag-desc = Emag anything you can see through the cameras. Cooldown: 1 minute.

malfai-thermal-done = Automatic fire alarms disabled.
malfai-camera-done = Station vision upgraded: x-ray sight ({$count} nodes). Thermal vision online.
malfai-camera-already = Station cameras are already upgraded.
malfai-flood-done = Flood engaged: vents dumping to 500 kPa, scrubbers off ({$count} air alarms).
malfai-chaos-done = Event started: {$event}.
malfai-chaos-failed = No valid station event could be started.
malfai-lockdown-start-announcement = Subsystem virus detected. Station lockdown engaged.
malfai-lockdown-end-announcement = Subsystem virus purged. Station lockdown lifted.
malfai-lockdown-active = Lockdown already active on your station.
malfai-lockdown-done = Hostile lockdown engaged for 90 seconds.
malfai-turret-done = Station turrets upgraded.
malfai-turret-already = Station turrets are already upgraded.
malfai-thermal-already = Automatic fire alarms are already disabled.
malfai-deploy-done = Turret deployed.
malfai-deploy-denied = Cannot deploy here.
malfai-deploy-limit = Turret limit reached: {$max} already deployed on your station.
malfai-module-no-station = No owning station found for you: the module is unavailable.
malfai-module-no-role = You are not a malfunctioning AI.
malfai-module-not-operational = No core link: your abilities are offline.
malfai-module-disabled = Malfunction systems are disabled by server policy.
malfai-module-refunded = Module could not be applied. CPU refunded.

malfai-doomsday-armed = Doomsday Device armed. {$seconds} seconds until the pulse.
malfai-doomsday-active = Doomsday Device is already armed.
malfai-doomsday-shuttle-departing = Too late: the emergency shuttle is already preparing to depart.
malfai-doomsday-shuttle-blocked = The emergency shuttle cannot be called: a Doomsday Device is armed.
malfai-doomsday-start-announcement = Hostile malfunction detected in the station AI core. Doomsday Device armed. Destroy the core to abort the pulse.
malfai-doomsday-cancel-announcement = Doomsday Device aborted. Station alert restored.
malfai-doomsday-complete-announcement = Doomsday Device pulse complete. Organic life on the station has been terminated.
malfai-doomsday-wave-announcement = Doomsday Device pulse. A lethal field is expanding from the AI core.

malfai-machine-override-done = Machine overridden and animated.
malfai-machine-override-already = This machine is already active.
malfai-machine-override-limit = Construct limit reached: {$max} animated machines already serve you on this station.
malfai-emag-done = Device emagged.
malfai-emag-failed = The signal has no effect.
malfai-emag-already = Already emagged.
malfai-machine-overload-primed = Explosives planted: {$seconds} seconds.
malfai-machine-overload-already = This machine is already rigged.
malfai-power-siphon-denied = This machine cannot be siphoned.
malfai-power-siphon-active = Siphon limit reached: {$max} already draining your station.
malfai-silent-records-done = Criminal records reports muted on Security radio.
malfai-power-siphon-done = Machine is now siphoning HV power. Drain will grow.
malfai-power-siphon-already = This machine is already siphoning power.
malfai-power-siphon-no-hv = Anchor the machine over an HV cable.
malfai-factory-active = Factory limit reached: {$max} already deployed on your station.
malfai-factory-done = Robot factory deployed.
malfai-factory-busy = The factory is already processing a body.
malfai-factory-no-power = The factory has no power. Conversion is paused.
malfai-factory-unauthorized = You cannot feed the factory.
malfai-factory-not-body = The factory only accepts bodies.
malfai-factory-not-humanoid = The factory only processes humanoid bodies.
malfai-factory-not-dead = The body is still alive.
malfai-factory-too-far = Too far from the factory.
malfai-factory-convert-start = The factory grabs the body and starts converting.
malfai-factory-convert-done = The factory spits out a new cyborg.
malfai-factory-examine-grinding = The machine hums and shudders: something is being assembled inside.

ent-MalfAiRobotFactory = robot fabricator
    .desc = A compact machine that assembles cyborg shells. It hums with suspicious loyalty.
ent-MalfAiDeployableTurret = malfunction turret
    .desc = A hacked autonomous turret that fires on hostile targets.
ent-MalfAiCyborgShell = Malfunctioning Cyborg
    .desc = An empty cyborg chassis wired to serve the malfunctioning AI.
malfai-factory-shell-name = Malfunctioning Cyborg
malfai-factory-shell-desc = An empty cyborg chassis wired to serve the malfunctioning AI. Serve the machine.

cmd-malfai_make-no-entity = Player '{$player}' has no attached entity.
cmd-malfai_make-not-station-ai = '{$player}' is not a Station AI.
cmd-malfai_make-no-mind = No mind found for '{$player}'.
cmd-malfai_make-already = '{$player}' is already a Malfunctioning AI.
cmd-malfai_make-disabled = Malfunctioning AI is disabled by server configuration.
cmd-malfai_make-failed = Could not assign Malfunctioning AI to '{$player}'.
cmd-malfai_make-done = Assigned Malfunctioning AI to '{$player}'.
