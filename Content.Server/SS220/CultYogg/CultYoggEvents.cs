namespace Content.Server.SS220.CultYogg
{
    /// <summary>
    ///     Event raised on entities when the stage of the cult is changed.
    /// </summary>
    [Serializable]
    public sealed class ChangeCultYoggStageEvent : EntityEventArgs
    {
        public int Stage;

        public ChangeCultYoggStageEvent(int stage)
        {
            Stage = stage;
        }
    }
}
