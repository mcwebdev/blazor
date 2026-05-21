namespace FlowBoard.Web.Components.UI;

public sealed record TaskFieldEditArgs(
    Guid BoardId,
    Guid TaskId,
    string FieldName);
