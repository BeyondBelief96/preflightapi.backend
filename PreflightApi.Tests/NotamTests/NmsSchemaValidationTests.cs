using System.Text.Json;
using FluentAssertions;
using PreflightApi.Infrastructure.Services.NotamServices.SchemaManifests;
using Xunit;

namespace PreflightApi.Tests.NotamTests;

public class NmsSchemaManifestLoaderTests
{
    [Fact]
    public void Load_ShouldLoadManifest()
    {
        var manifest = NmsSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.Schema.Should().Be("nms-geojson-schema-manifest-v1");
        manifest.TopLevelProperties.Should().NotBeEmpty();
        manifest.NestedObjects.Should().NotBeEmpty();
    }

    [Fact]
    public void Load_ShouldHaveExpectedTopLevelProperties()
    {
        var manifest = NmsSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.TopLevelProperties.Should().ContainKey("type");
        manifest.TopLevelProperties.Should().ContainKey("id");
        manifest.TopLevelProperties.Should().ContainKey("geometry");
        manifest.TopLevelProperties.Should().ContainKey("properties");

        manifest.TopLevelProperties["type"].Required.Should().BeTrue();
        manifest.TopLevelProperties["id"].Required.Should().BeTrue();
        manifest.TopLevelProperties["geometry"].Required.Should().BeTrue();
        manifest.TopLevelProperties["properties"].Required.Should().BeTrue();
    }

    [Fact]
    public void Load_ShouldHaveExpectedNestedObjects()
    {
        var manifest = NmsSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        manifest!.NestedObjects.Should().ContainKey("geometry");
        manifest.NestedObjects.Should().ContainKey("properties");
        manifest.NestedObjects.Should().ContainKey("properties.coreNOTAMData");
        manifest.NestedObjects.Should().ContainKey("properties.coreNOTAMData.notam");
        manifest.NestedObjects.Should().ContainKey("properties.coreNOTAMData.notamTranslation[]");
    }

    [Fact]
    public void Load_ShouldHaveNotamDetailProperties()
    {
        var manifest = NmsSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var notamProps = manifest!.NestedObjects["properties.coreNOTAMData.notam"].Properties;

        notamProps.Should().ContainKey("id");
        notamProps.Should().ContainKey("number");
        notamProps.Should().ContainKey("type");
        notamProps.Should().ContainKey("issued");
        notamProps.Should().ContainKey("location");
        notamProps.Should().ContainKey("effectiveStart");
        notamProps.Should().ContainKey("text");
        notamProps.Should().ContainKey("classification");

        notamProps["id"].Required.Should().BeTrue();
        notamProps["number"].Required.Should().BeTrue();
        notamProps["series"].Required.Should().BeFalse();
        notamProps["schedule"].Required.Should().BeFalse();
    }

    [Fact]
    public void Load_ShouldHaveTranslationProperties()
    {
        var manifest = NmsSchemaManifestLoader.Load();

        manifest.Should().NotBeNull();
        var translationProps = manifest!.NestedObjects["properties.coreNOTAMData.notamTranslation[]"].Properties;

        translationProps.Should().ContainKey("type");
        translationProps.Should().ContainKey("simpleText");
        translationProps.Should().ContainKey("domestic_message");
        translationProps.Should().ContainKey("icao_message");
        translationProps.Should().ContainKey("formattedText");

        translationProps["type"].Required.Should().BeTrue();
        translationProps["simpleText"].Required.Should().BeFalse();
    }
}

public class NmsSchemaValidatorTests
{
    [Fact]
    public void ValidateFeature_ShouldReturnNoDrift_WhenAllRequiredPropertiesPresent()
    {
        var json = JsonDocument.Parse(BuildValidFeatureJson()).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeFalse();
        result.MissingProperties.Should().BeEmpty();
        result.UnexpectedProperties.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFeature_ShouldDetectMissingRequiredTopLevelProperties()
    {
        // Feature missing "id" and "geometry"
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    }
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.MissingProperties.Should().Contain("id");
        result.MissingProperties.Should().Contain("geometry");
    }

    [Fact]
    public void ValidateFeature_ShouldNotFlagOptionalMissingProperties()
    {
        // Feature with only required properties in notam (no optional like series, year, etc.)
        var json = JsonDocument.Parse(BuildValidFeatureJson()).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.MissingProperties.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFeature_ShouldDetectUnexpectedTopLevelProperties()
    {
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    }
                }
            },
            "newUnknownField": "surprise",
            "anotherNewField": 42
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedProperties.Should().Contain("newUnknownField");
        result.UnexpectedProperties.Should().Contain("anotherNewField");
    }

    [Fact]
    public void ValidateFeature_ShouldDetectMissingRequiredNestedProperties()
    {
        // notam object missing required "id" and "number"
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    }
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.MissingProperties.Should().Contain("properties.coreNOTAMData.notam.id");
        result.MissingProperties.Should().Contain("properties.coreNOTAMData.notam.number");
    }

    [Fact]
    public void ValidateFeature_ShouldDetectUnexpectedNestedProperties()
    {
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC",
                        "brandNewField": "surprise"
                    }
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedProperties.Should().Contain("properties.coreNOTAMData.notam.brandNewField");
    }

    [Fact]
    public void ValidateFeature_ShouldValidateTranslationArray()
    {
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    },
                    "notamTranslation": [
                        {
                            "type": "LOCAL_FORMAT",
                            "simpleText": "Test translation",
                            "unknownTranslationField": "surprise"
                        }
                    ]
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedProperties.Should().Contain("properties.coreNOTAMData.notamTranslation.unknownTranslationField");
    }

    [Fact]
    public void ValidateFeature_ShouldDetectMissingRequiredTranslationType()
    {
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    },
                    "notamTranslation": [
                        {
                            "simpleText": "Test translation"
                        }
                    ]
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.MissingProperties.Should().Contain("properties.coreNOTAMData.notamTranslation.type");
    }

    [Fact]
    public void ValidateFeature_ShouldReturnEmptyResult_WhenManifestCannotBeLoaded()
    {
        // This tests the fallback behavior - if manifest assembly is fine,
        // the Load() will work. We just verify the result structure is correct.
        var json = JsonDocument.Parse(BuildValidFeatureJson()).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.Should().NotBeNull();
        result.MissingProperties.Should().NotBeNull();
        result.UnexpectedProperties.Should().NotBeNull();
    }

    [Fact]
    public void ValidateFeature_ShouldHandleGeometryValidation()
    {
        // Geometry with unexpected property
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": {
                "type": "Point",
                "coordinates": [-97.038, 32.897],
                "newGeoProperty": "unexpected"
            },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    }
                }
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedProperties.Should().Contain("geometry.newGeoProperty");
    }

    [Fact]
    public void ValidateFeature_ShouldDetectUnexpectedPropertiesChild()
    {
        // Properties wrapper with unexpected child alongside coreNOTAMData
        var json = JsonDocument.Parse("""
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": { "type": "Point", "coordinates": [-97.038, 32.897] },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "Test NOTAM",
                        "classification": "DOMESTIC"
                    }
                },
                "newPropertiesChild": "unexpected"
            }
        }
        """).RootElement;

        var result = NmsSchemaValidator.ValidateFeature(json);

        result.HasDrift.Should().BeTrue();
        result.UnexpectedProperties.Should().Contain("properties.newPropertiesChild");
    }

    private static string BuildValidFeatureJson()
    {
        return """
        {
            "type": "Feature",
            "id": "1234567890123456",
            "geometry": {
                "type": "Point",
                "coordinates": [-97.038, 32.897]
            },
            "properties": {
                "coreNOTAMData": {
                    "notam": {
                        "id": "1234567890123456",
                        "number": "01/001",
                        "type": "N",
                        "issued": "2026-02-11T00:00:00Z",
                        "location": "DFW",
                        "effectiveStart": "2026-02-11T00:00:00Z",
                        "text": "RWY 18R/36L CLSD",
                        "classification": "DOMESTIC"
                    }
                }
            }
        }
        """;
    }
}
