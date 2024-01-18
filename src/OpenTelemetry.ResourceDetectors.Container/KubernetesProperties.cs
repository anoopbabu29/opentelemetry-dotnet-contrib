// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
namespace OpenTelemetry.ResourceDetectors.Container;

internal class KubernetesProperties(
    string kubernetesPortEnvVar = "KUBERNETES_PORT",
    string kubernetesServiceHostEnvVar = "KUBERNETES_SERVICE_HOST",
    string kubernetesServicePortEnvVar = "KUBERNETES_SERVICE_PORT",
    string hostnameEnvVar = "HOSTNAME",
    string containerNameEnvVar = "CONTAINER_NAME",
    string containerNameEnvVar2 = "container.name",
    string kodNamespaceEnvVar = "NAMESPACE",
    string kubeServiceAcctDirPath = "/var/run/secrets/kubernetes.io/serviceaccount",
    string kubeApiCertFile = "ca.crt",
    string kubeApiTokenFile = "token",
    string kubeApiNamespaceFile = "namespace")
{
    public string KubernetesPortEnvVar = kubernetesPortEnvVar;
    public string KubernetesServiceHostEnvVar = kubernetesServiceHostEnvVar;
    public string KubernetesServicePortEnvVar = kubernetesServicePortEnvVar;

    public string HostnameEnvVar = hostnameEnvVar;

    public string ContainerNameEnvVar = containerNameEnvVar;
    public string ContainerNameEnvVar2 = containerNameEnvVar2;

    public string PodNamespaceEnvVar = kodNamespaceEnvVar;

    public string KubeServiceAcctDirPath = kubeServiceAcctDirPath;
    public string KubeApiCertFile = kubeApiCertFile;
    public string KubeApiTokenFile = kubeApiTokenFile;
    public string KubeApiNamespaceFile = kubeApiNamespaceFile;
}
