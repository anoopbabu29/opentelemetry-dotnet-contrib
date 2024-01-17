// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !NETFRAMEWORK
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
#endif
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.ResourceDetectors.Container.Tests;

public class ContainerResourceDetectorTests : IDisposable
{
    // Kubernetes Test Environment Variables
    private const string KUBESERVICEHOST = "127.0.0.1";
    private const string KUBESERVICEPORT = "0";
    private const string HOSTNAME = "demo";
    private const string CONTAINERNAME = "test2";
    private const string TESTNAMESPACE = "default";
    private const string KUBEEXPECTEDCONTAINERID = "cd9db70ac37bca61b7037406c01f79b9888550ca57c66d901ce063c02aa4ac29";

    // contains a "z"
    private const string KUBEINVALIDCONTAINERID = "fb5916a02feca96bdeecd8e062df9e5e51d6617c8214b5e1f3fz9320f4402ae6";

    private readonly List<TestCase> testValidCasesV1 =
    [
        new(
            name: "cgroupv1 with prefix",
            line: "13:name=systemd:/podruntime/docker/kubepods/crio-e2cc29debdf85dde404998aa128997a819ff",
            expectedContainerId: "e2cc29debdf85dde404998aa128997a819ff",
            cgroupVersion: ContainerResourceDetector.ParseMode.V1),
        new(
            name: "cgroupv1 with suffix",
            line: "13:name=systemd:/podruntime/docker/kubepods/ac679f8a8319c8cf7d38e1adf263bc08d23.aaaa",
            expectedContainerId: "ac679f8a8319c8cf7d38e1adf263bc08d23",
            cgroupVersion: ContainerResourceDetector.ParseMode.V1),
        new(
            name: "cgroupv1 with prefix and suffix",
            line: "13:name=systemd:/podruntime/docker/kubepods/crio-dc679f8a8319c8cf7d38e1adf263bc08d23.stuff",
            expectedContainerId: "dc679f8a8319c8cf7d38e1adf263bc08d23",
            cgroupVersion: ContainerResourceDetector.ParseMode.V1),
        new(
            name: "cgroupv1 with container Id",
            line: "13:name=systemd:/pod/d86d75589bf6cc254f3e2cc29debdf85dde404998aa128997a819ff991827356",
            expectedContainerId: "d86d75589bf6cc254f3e2cc29debdf85dde404998aa128997a819ff991827356",
            cgroupVersion: ContainerResourceDetector.ParseMode.V1),
    ];

    private readonly List<TestCase> testValidCasesV2 =
    [
        new(
            name: "cgroupv2 with container Id",
            line: "13:name=systemd:/pod/d86d75589bf6cc254f3e2cc29debdf85dde404998aa128997a819ff991827356/hostname",
            expectedContainerId: "d86d75589bf6cc254f3e2cc29debdf85dde404998aa128997a819ff991827356",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
        new(
            name: "cgroupv2 with full line",
            line: "473 456 254:1 /docker/containers/dc64b5743252dbaef6e30521c34d6bbd1620c8ce65bdb7bf9e7143b61bb5b183/hostname /etc/hostname rw,relatime - ext4 /dev/vda1 rw",
            expectedContainerId: "dc64b5743252dbaef6e30521c34d6bbd1620c8ce65bdb7bf9e7143b61bb5b183",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
        new(
            name: "cgroupv2 with minikube containerd mountinfo",
            line: "1537 1517 8:1 /var/lib/containerd/io.containerd.grpc.v1.cri/sandboxes/fb5916a02feca96bdeecd8e062df9e5e51d6617c8214b5e1f3ff9320f4402ae6/hostname /etc/hostname rw,relatime - ext4 /dev/sda1 rw",
            expectedContainerId: "fb5916a02feca96bdeecd8e062df9e5e51d6617c8214b5e1f3ff9320f4402ae6",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
        new(
            name: "cgroupv2 with minikube docker mountinfo",
            line: "2327 2307 8:1 /var/lib/docker/containers/a1551a1d7e1881d6c18d2c9ec462cab6ad3666825f0adb2098e9d5b198fd7e19/hostname /etc/hostname rw,relatime - ext4 /dev/sda1 rw",
            expectedContainerId: "a1551a1d7e1881d6c18d2c9ec462cab6ad3666825f0adb2098e9d5b198fd7e19",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
        new(
            name: "cgroupv2 with minikube docker mountinfo2",
            line: "929 920 254:1 /docker/volumes/minikube/_data/lib/docker/containers/0eaa6718003210b6520f7e82d14b4c8d4743057a958a503626240f8d1900bc33/hostname /etc/hostname rw,relatime - ext4 /dev/vda1 rw",
            expectedContainerId: "0eaa6718003210b6520f7e82d14b4c8d4743057a958a503626240f8d1900bc33",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
        new(
            name: "cgroupv2 with podman mountinfo",
            line: "1096 1088 0:104 /containers/overlay-containers/1a2de27e7157106568f7e081e42a8c14858c02bd9df30d6e352b298178b46809/userdata/hostname /etc/hostname rw,nosuid,nodev,relatime - tmpfs tmpfs rw,size=813800k,nr_inodes=203450,mode=700,uid=1000,gid=1000",
            expectedContainerId: "1a2de27e7157106568f7e081e42a8c14858c02bd9df30d6e352b298178b46809",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
    ];

    private readonly List<TestCase> testInvalidCases =
    [
        new(
            name: "Invalid cgroupv1 line",
            line: "13:name=systemd:/podruntime/docker/kubepods/ac679f8a8319c8cf7d38e1adf263bc08d23zzzz",
            cgroupVersion: ContainerResourceDetector.ParseMode.V1),
        new(
            name: "Invalid hex cgroupv2 line (contains a z)",
            line: "13:name=systemd:/var/lib/containerd/io.containerd.grpc.v1.cri/sandboxes/fb5916a02feca96bdeecd8e062df9e5e51d6617c8214b5e1f3fz9320f4402ae6/hostname",
            cgroupVersion: ContainerResourceDetector.ParseMode.V2),
    ];

    private readonly string originalKubeServiceAcctDirPath = KubernetesProperties.KubeServiceAcctDirPath;
    private readonly string originalKubeApiCertFile = KubernetesProperties.KubeApiCertFile;
    private readonly string originalKubeApiTokenFile = KubernetesProperties.KubeApiTokenFile;
    private readonly string originalKubeApiNamespaceFile = KubernetesProperties.KubeApiNamespaceFile;

    private TempFile? kubeNamespaceFile;
    private TempFile? kubeTokenFile;
    private TempFile? kubeCertFile;

    public ContainerResourceDetectorTests()
    {
        this.ResetEnvironment();
    }

    public void Dispose()
    {
        this.ResetEnvironment();
    }

    [Fact]
    public void TestValidContainer()
    {
        var containerResourceDetector = new ContainerResourceDetector();
        var allValidTestCases = this.testValidCasesV1.Concat(this.testValidCasesV2);

        foreach (var testCase in allValidTestCases)
        {
            using var tempFile = new TempFile();
            tempFile.Write(testCase.Line);
            Assert.Equal(
                testCase.ExpectedContainerId,
                GetContainerId(containerResourceDetector.BuildResource(tempFile.FilePath, testCase.CgroupVersion)));
        }
    }

    [Fact]
    public void TestInvalidContainer()
    {
        var containerResourceDetector = new ContainerResourceDetector();

        // Valid in cgroupv1 is not valid in cgroupv2
        foreach (var testCase in this.testValidCasesV1)
        {
            using var tempFile = new TempFile();
            tempFile.Write(testCase.Line);
            Assert.Equal(
                containerResourceDetector.BuildResource(tempFile.FilePath, ContainerResourceDetector.ParseMode.V2),
                Resource.Empty);
        }

        // Valid in cgroupv1 is not valid in cgroupv1
        foreach (var testCase in this.testValidCasesV2)
        {
            using var tempFile = new TempFile();
            tempFile.Write(testCase.Line);
            Assert.Equal(
                containerResourceDetector.BuildResource(tempFile.FilePath, ContainerResourceDetector.ParseMode.V1),
                Resource.Empty);
        }

        // test invalid cases
        foreach (var testCase in this.testInvalidCases)
        {
            using var tempFile = new TempFile();
            tempFile.Write(testCase.Line);
            Assert.Equal(containerResourceDetector.BuildResource(tempFile.FilePath, testCase.CgroupVersion), Resource.Empty);
        }

        // test invalid file
        Assert.Equal(containerResourceDetector.BuildResource(Path.GetTempPath(), ContainerResourceDetector.ParseMode.V1), Resource.Empty);
        Assert.Equal(containerResourceDetector.BuildResource(Path.GetTempPath(), ContainerResourceDetector.ParseMode.V2), Resource.Empty);
    }

#if !NETFRAMEWORK
    [Fact]
    public async void TestValidKubeContainer()
    {
        this.SetKubeEnvironment();
        var containerResourceDetector = new ContainerResourceDetector();

        await using var metadataEndpoint = new MockKubeApiEndpoint(KUBEEXPECTEDCONTAINERID);

        Assert.Equal(
            KUBEEXPECTEDCONTAINERID,
            GetContainerId(containerResourceDetector.BuildResource(Path.GetTempPath(), ContainerResourceDetector.ParseMode.K8)));
    }

    [Fact]
    public async void TestInvalidKubeContainer()
    {
        this.SetKubeEnvironment();
        var containerResourceDetector = new ContainerResourceDetector();
        await using (var metadataEndpoint = new MockKubeApiEndpoint(KUBEINVALIDCONTAINERID))
        {
            Assert.Equal(containerResourceDetector.BuildResource(Path.GetTempPath(), ContainerResourceDetector.ParseMode.K8), Resource.Empty);
        }

        await using (var metadataEndpoint = new MockKubeApiEndpoint(KUBEEXPECTEDCONTAINERID, false))
        {
            Assert.Equal(containerResourceDetector.BuildResource(Path.GetTempPath(), ContainerResourceDetector.ParseMode.K8), Resource.Empty);
        }
    }
#endif

    private static string GetContainerId(Resource resource)
    {
        var resourceAttributes = resource.Attributes.ToDictionary(x => x.Key, x => x.Value);
        return resourceAttributes[ContainerSemanticConventions.AttributeContainerId].ToString()!;
    }

    private void SetKubeEnvironment()
    {
        Environment.SetEnvironmentVariable(KubernetesProperties.KubernetesServiceHostEnvVar, KUBESERVICEHOST);
        Environment.SetEnvironmentVariable(KubernetesProperties.KubernetesServicePortEnvVar, KUBESERVICEPORT);
        Environment.SetEnvironmentVariable(KubernetesProperties.HostnameEnvVar, HOSTNAME);
        Environment.SetEnvironmentVariable(KubernetesProperties.ContainerNameEnvVar, CONTAINERNAME);

        this.kubeCertFile = new TempFile();
        this.kubeTokenFile = new TempFile();
        this.kubeNamespaceFile = new TempFile();

        this.kubeCertFile.Write("Test Certificate");
        this.kubeTokenFile.Write("Test Token");
        this.kubeNamespaceFile.Write(TESTNAMESPACE);

        KubernetesProperties.KubeServiceAcctDirPath = string.Empty;
        KubernetesProperties.KubeApiCertFile = this.kubeCertFile.FilePath;
        KubernetesProperties.KubeApiTokenFile = this.kubeTokenFile.FilePath;
        KubernetesProperties.KubeApiNamespaceFile = this.kubeNamespaceFile.FilePath;
    }

    private void ResetEnvironment()
    {
        Environment.SetEnvironmentVariable(KubernetesProperties.KubernetesServiceHostEnvVar, null);
        Environment.SetEnvironmentVariable(KubernetesProperties.KubernetesServicePortEnvVar, null);
        Environment.SetEnvironmentVariable(KubernetesProperties.HostnameEnvVar, null);
        Environment.SetEnvironmentVariable(KubernetesProperties.ContainerNameEnvVar, null);

        this.kubeNamespaceFile?.Dispose();
        this.kubeTokenFile?.Dispose();
        this.kubeCertFile?.Dispose();

        KubernetesProperties.KubeServiceAcctDirPath = this.originalKubeServiceAcctDirPath;
        KubernetesProperties.KubeApiCertFile = this.originalKubeApiCertFile;
        KubernetesProperties.KubeApiTokenFile = this.originalKubeApiTokenFile;
        KubernetesProperties.KubeApiNamespaceFile = this.originalKubeApiNamespaceFile;
    }

#if !NETFRAMEWORK
    private class MockKubeApiEndpoint : IAsyncDisposable
    {
        public readonly Uri Address;
        private readonly IWebHost server;

        public MockKubeApiEndpoint(string containerId, bool isFound = true)
        {
            // TODO: Update with default namespace name
            this.server = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"https://{KUBESERVICEHOST}:{KUBESERVICEPORT}") // Use random localhost port
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        if (context.Request.Method == HttpMethods.Get && context.Request.Path == $"/api/v1/namespaces/{TESTNAMESPACE}/pods/{HOSTNAME}" && isFound)
                        {
                            var content = await File.ReadAllTextAsync("SampleKubeApiResponses/SampleResponseTemplate.json");
                            content = content.Replace("#expectedContainerId#", containerId);
                            var data = Encoding.UTF8.GetBytes(content);
                            context.Response.ContentType = "application/json";
                            await context.Response.Body.WriteAsync(data);
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            await context.Response.WriteAsync("Not found");
                        }
                    });
                }).Build();
            this.server.Start();

            this.Address = new Uri(this.server.ServerFeatures.Get<IServerAddressesFeature>()!.Addresses.First());
        }

        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await this.server.StopAsync();
        }
    }
#endif

    private sealed class TestCase(string name, string line, ContainerResourceDetector.ParseMode cgroupVersion, string? expectedContainerId = null)
    {
        public string Name { get; } = name;

        public string Line { get; } = line;

        public string? ExpectedContainerId { get; } = expectedContainerId;

        public ContainerResourceDetector.ParseMode CgroupVersion { get; } = cgroupVersion;
    }
}
