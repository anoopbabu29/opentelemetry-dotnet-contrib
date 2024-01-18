// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace OpenTelemetry.ResourceDetectors.Container.Models;

internal class ContainerStatus
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("containerID")]
    public string? ContainerID { get; set; }
}
