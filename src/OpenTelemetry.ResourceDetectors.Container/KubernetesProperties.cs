// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenTelemetry.ResourceDetectors.Container;

internal static class KubernetesProperties
{
    public static string KubernetesPortEnvVar = "KUBERNETES_PORT";
    public static string KubernetesServiceHostEnvVar = "KUBERNETES_SERVICE_HOST";
    public static string KubernetesServicePortEnvVar = "KUBERNETES_SERVICE_PORT";

    public static string HostnameEnvVar = "HOSTNAME";

    public static string ContainerNameEnvVar = "CONTAINER_NAME";
    public static string ContainerNameEnvVar2 = "container.name";

    public static string PodNamespaceEnvVar = "NAMESPACE";

    public static string KubeServiceAcctDirPath = "/var/run/secrets/kubernetes.io/serviceaccount";
    public static string KubeApiCertFile = "ca.crt";
    public static string KubeApiTokenFile = "token";
    public static string KubeApiNamespaceFile = "namespace";

    // Classes exist for Newtonsoft Deserializing
    public class Pod
    {
        [JsonPropertyName("status")]
        public PodStatus? Status { get; set; }
    }

    public class PodStatus
    {
        [JsonPropertyName("containerStatuses")]
        public List<ContainerStatus>? ContainerStatuses { get; set; }
    }

    public class ContainerStatus
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("containerID")]
        public string? ContainerID { get; set; }
    }
}
