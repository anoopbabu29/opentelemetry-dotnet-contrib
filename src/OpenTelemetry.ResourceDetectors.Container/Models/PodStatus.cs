// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenTelemetry.ResourceDetectors.Container.Models;

internal class PodStatus
{
    [JsonPropertyName("containerStatuses")]
    public List<ContainerStatus>? ContainerStatuses { get; set; }
}
