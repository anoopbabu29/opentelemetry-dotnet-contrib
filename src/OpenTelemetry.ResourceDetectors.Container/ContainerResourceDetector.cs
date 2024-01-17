// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OpenTelemetry.ResourceDetectors.Container.Utils;
using OpenTelemetry.Resources;

namespace OpenTelemetry.ResourceDetectors.Container;

/// <summary>
/// Resource detector for application running in Container environment.
/// </summary>
public class ContainerResourceDetector : IResourceDetector
{
    private const string Filepath = "/proc/self/cgroup";
    private const string FilepathV2 = "/proc/self/mountinfo";
    private const string Hostname = "hostname";

    /// <summary>
    /// CGroup Parse Versions.
    /// </summary>
    internal enum ParseMode
    {
        /// <summary>
        /// Represents CGroupV1.
        /// </summary>
        V1,

        /// <summary>
        /// Represents CGroupV2.
        /// </summary>
        V2,

        /// <summary>
        /// Represents Kubernetes.
        /// </summary>
        K8,
    }

    /// <summary>
    /// Detects the resource attributes from Container.
    /// </summary>
    /// <returns>Resource with key-value pairs of resource attributes.</returns>
    public Resource Detect()
    {
        var cGroupBuild = this.BuildResource(Filepath, ParseMode.K8);
        if (cGroupBuild == Resource.Empty)
        {
            cGroupBuild = this.BuildResource(Filepath, ParseMode.V1);
        }

        if (cGroupBuild == Resource.Empty)
        {
            cGroupBuild = this.BuildResource(FilepathV2, ParseMode.V2);
        }

        return cGroupBuild;
    }

    /// <summary>
    /// Builds the resource attributes from Container Id in file path.
    /// </summary>
    /// <param name="path">File path where container id exists.</param>
    /// <param name="parseType">CGroup Version of file to parse from or Kubernetes version.</param>
    /// <returns>Returns Resource with list of key-value pairs of container resource attributes if container id exists else empty resource.</returns>
    internal Resource BuildResource(string path, ParseMode parseType)
    {
        var containerId = this.ExtractContainerId(path, parseType);

        if (string.IsNullOrEmpty(containerId))
        {
            return Resource.Empty;
        }
        else
        {
            return new Resource(new List<KeyValuePair<string, object>>(1) { new(ContainerSemanticConventions.AttributeContainerId, containerId!) });
        }
    }

    /// <summary>
    /// Gets the Container Id from the line after removing the prefix and suffix from the cgroupv1 format.
    /// </summary>
    /// <param name="line">line read from cgroup file.</param>
    /// <returns>Container Id, Null if not found.</returns>
    private static string? GetIdFromLineV1(string line)
    {
        // This cgroup output line should have the container id in it
        int lastSlashIndex = line.LastIndexOf('/');
        if (lastSlashIndex < 0)
        {
            return null;
        }

        string lastSection = line.Substring(lastSlashIndex + 1);
        int startIndex = lastSection.LastIndexOf('-');
        int endIndex = lastSection.LastIndexOf('.');

        string containerId = RemovePrefixAndSuffixIfNeeded(lastSection, startIndex, endIndex);

        if (string.IsNullOrEmpty(containerId) || !EncodingUtils.IsValidHexString(containerId))
        {
            return null;
        }

        return containerId;
    }

    /// <summary>
    /// Gets the Container Id from the line of the cgroupv2 format.
    /// </summary>
    /// <param name="line">line read from cgroup file.</param>
    /// <returns>Container Id, Null if not found.</returns>
    private static string? GetIdFromLineV2(string line)
    {
        string? containerId = null;
        var match = Regex.Match(line, @".*/.+/([\w+-.]{64})/.*$");
        if (match.Success)
        {
            containerId = match.Groups[1].Value;
        }

        if (string.IsNullOrEmpty(containerId) || !EncodingUtils.IsValidHexString(containerId!))
        {
            return null;
        }

        return containerId;
    }

    private static string? ExtractContainerIdK8()
    {
#if !NETFRAMEWORK
        KubernetesContainerInfoFetcher? containerInfoFetcher = KubernetesContainerInfoFetcher.GetInstance();
        if (containerInfoFetcher != null)
        {
            string kubeContainerId = containerInfoFetcher.ExtractContainerId();
            if (!string.IsNullOrEmpty(kubeContainerId) || !EncodingUtils.IsValidHexString(kubeContainerId))
            {
                return kubeContainerId;
            }
        }
#endif

        return null;
    }

    private static string RemovePrefixAndSuffixIfNeeded(string input, int startIndex, int endIndex)
    {
        startIndex = (startIndex == -1) ? 0 : startIndex + 1;

        if (endIndex == -1)
        {
            endIndex = input.Length;
        }

        return input.Substring(startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// Extracts Container Id from path using the cgroupv1 format.
    /// </summary>
    /// <param name="path">cgroup path.</param>
    /// <param name="parseType">CGroup Version of file to parse from or Kubernetes version.</param>
    /// <returns>Container Id, Null if not found or exception being thrown.</returns>
    private string? ExtractContainerId(string path, ParseMode parseType)
    {
        try
        {
            if (parseType == ParseMode.K8)
            {
                return ExtractContainerIdK8();
            }
            else
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                foreach (string line in File.ReadLines(path))
                {
                    string? containerId = null;
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (parseType == ParseMode.V1)
                        {
                            containerId = GetIdFromLineV1(line);
                        }
                        else if (parseType == ParseMode.V2 && line.Contains(Hostname))
                        {
                            containerId = GetIdFromLineV2(line);
                        }
                    }

                    if (!string.IsNullOrEmpty(containerId))
                    {
                        return containerId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ContainerExtensionsEventSource.Log.ExtractResourceAttributesException($"{nameof(ContainerResourceDetector)} : Failed to extract Container id from path", ex);
        }

        return null;
    }
}
