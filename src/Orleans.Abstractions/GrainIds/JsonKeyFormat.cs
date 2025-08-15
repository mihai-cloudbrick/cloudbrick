namespace Cloudbrick.Orleans.Abstractions.GrainIds;

public enum JsonKeyFormat
{
    PlainJson,   // e.g. {"TenantId":"t1","UserId":"u1"}
    Base64Json,  // e.g. eyJUZW5hbnRJZCI6InQxIiwiVXNlcklkIjoidTEifQ==
    Auto         // Parse: detect Base64 if it’s not obvious JSON
}