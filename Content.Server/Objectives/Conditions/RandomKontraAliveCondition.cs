using System.Linq;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed class RandomKontraAliveCondition : IObjectiveCondition
    {
        private Mind.Mind? _target;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            var kontras = entityMgr.EntitySysManager.GetEntitySystem<KontrRazvedchikRuleSystem>().GetOtherKontrasAliveAndConnected(mind).ToList();
            if (kontras.Count == 0)
                return new EscapeShuttleCondition(); //You were made a kontra by admins, and are the first/only.

            return new RandomKontraAliveCondition { _target = IoCManager.Resolve<IRobustRandom>().Pick(kontras).Mind };
        }

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var jobName = _target?.CurrentJob?.Name ?? "Unknown";

                if (_target == null)
                    return Loc.GetString("objective-condition-other-kontra-alive-title", ("targetName", targetName), ("job", jobName));

                if (_target.OwnedEntity is { Valid: true } owned)
                    targetName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-other-kontra-alive-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-other-kontra-alive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress
        {
            get
            {
                var entityManager = IoCManager.Resolve<EntityManager>();
                var mindSystem = entityManager.System<MindSystem>();
                return _target == null || !mindSystem.IsCharacterDeadIc(_target) ? 1f : 0f;
            }
        }

        public float Difficulty => 1.75f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is RandomKontraAliveCondition kpc && Equals(_target, kpc._target);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RandomKontraAliveCondition alive && alive.Equals(this);
        }

        public override int GetHashCode()
        {
            return _target?.GetHashCode() ?? 0;
        }
    }
}
