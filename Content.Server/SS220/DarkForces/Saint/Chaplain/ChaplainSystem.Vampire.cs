// using Content.Server.SS220.DarkForces.Saint.Chaplain.Components;
// using Content.Shared.SS220.Vampire.Attempt;
// using Robust.Shared.GameObjects;

// namespace Content.Server.SS220.DarkForces.Saint.Chaplain;

// public sealed partial class ChaplainSystem
// {
//     private void InitializeVampire()
//     {
//         SubscribeLocalEvent<ChaplainComponent, VampireChiropteanScreechAttemptEvent>(OnVampireScreechAttemptEvent);
//         SubscribeLocalEvent<ChaplainComponent, VampireHypnosisAttemptEvent>(OnVampireHypnosisAttemptEvent);
//         SubscribeLocalEvent<ChaplainComponent, VampireParalizeAttemptEvent>(OnVampireParalyzeAttemptEvent);
//     }

//     private void OnVampireParalyzeAttemptEvent(EntityUid uid, ChaplainComponent component, VampireParalizeAttemptEvent args)
//     {
//         if (args.FullPower)
//             return;

//         args.Cancel();
//     }

//     private void OnVampireHypnosisAttemptEvent(EntityUid uid, ChaplainComponent component,
//         VampireHypnosisAttemptEvent args)
//     {
//         if (args.FullPower)
//             return;

//         args.Cancel();
//     }

//     private void OnVampireScreechAttemptEvent(EntityUid uid, ChaplainComponent component, VampireChiropteanScreechAttemptEvent args)
//     {
//         if (args.FullPower)
//             return;

//         args.Cancel();
//     }
// }
