namespace RetailForecast.DTOs.Dataset
{
    public record DatasetFeatureCorrelationResponse(
        string LeftColumn,
        string RightColumn,
        double Correlation,
        double AbsoluteCorrelation,
        string Severity
    );
}
