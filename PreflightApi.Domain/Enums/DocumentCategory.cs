using System.Text.Json.Serialization;

namespace PreflightApi.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentCategory
{
    POH,           // Pilot's Operating Handbook
    Manual,        // Equipment manuals, avionics guides
    Checklist,     // Custom checklists
    Maintenance,   // Maintenance records, logs
    Insurance,     // Insurance documents
    Registration,  // Aircraft registration
    Other          // Catch-all
}
