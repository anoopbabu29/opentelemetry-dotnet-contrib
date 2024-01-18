// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace OpenTelemetry.ResourceDetectors.Container.Models;

internal class KubePod
{
    [JsonPropertyName("status")]
    public PodStatus? Status { get; set; }
}
