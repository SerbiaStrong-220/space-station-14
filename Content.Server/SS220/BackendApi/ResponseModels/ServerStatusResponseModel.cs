namespace Content.Server.SS220.BackendApi.ResponseModels;

internal sealed class ServerStatusResponseModel
{
    public int PlayersCount { get; set; }

    public TimeSpan RoundDuration { get; set; }

    public int AdminCount { get; set; }
}
