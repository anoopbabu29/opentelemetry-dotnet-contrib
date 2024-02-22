// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics.Tracing;
using System.Net.Security;
using OpenTelemetry.Internal;

namespace OpenTelemetry.ResourceDetectors.AWS;

[EventSource(Name = "OpenTelemetry-ResourceDetectors-AWS")]
internal sealed class AWSResourcesEventSource : EventSource, IServerCertificateValidationEventSource
{
    public static AWSResourcesEventSource Log = new();
    private const int EVENT_ID_ATTR_ERROR = 1;
    private const int EVENT_ID_CERT_VALIDATATION_ERR = 2;
    private const int EVENT_ID_INVALID_CERT = 3;
    private const int EVENT_ID_INVALID_CERT_CHAIN = 4;
    private const int EVENT_ID_SSL_POLICY_ERROR = 5;
    private const int EVENT_ID_UNTRUSTED_CERT_ERROR = 6;

    [NonEvent]
    public void ResourceAttributesExtractException(string format, Exception ex)
    {
        if (this.IsEnabled(EventLevel.Warning, (EventKeywords)(-1)))
        {
            this.FailedToExtractResourceAttributes(format, ex.ToInvariantString());
        }
    }

    [Event(1, Message = "Failed to extract resource attributes in '{0}'.", Level = EventLevel.Warning)]
    public void FailedToExtractResourceAttributes(string format, string exception)
    {
        this.WriteEvent(EVENT_ID_ATTR_ERROR, format, exception);
    }

    [Event(EVENT_ID_CERT_VALIDATATION_ERR, Message = "Failed to Validate Certificate. Error: '{0}'.", Level = EventLevel.Warning)]
    public void FailedToValidateCertificate(string error)
    {
        this.WriteEvent(EVENT_ID_CERT_VALIDATATION_ERR, error);
    }

    [Event(EVENT_ID_INVALID_CERT, Message = "Invalid Certificate. File Name: '{0}'.", Level = EventLevel.Warning)]
    public void InvalidCertificateError(string fileName)
    {
        this.WriteEvent(EVENT_ID_INVALID_CERT, fileName);
    }

    [Event(EVENT_ID_INVALID_CERT_CHAIN, Message = "Invalid Certificate Chain. Error: '{0}'.", Level = EventLevel.Warning)]
    public void InvalidCertificateChainError(string error)
    {
        this.WriteEvent(EVENT_ID_INVALID_CERT_CHAIN, error);
    }

    [Event(EVENT_ID_SSL_POLICY_ERROR, Message = "SslPolicyError: '{0}'.", Level = EventLevel.Warning)]
    public void SslPolicyError(SslPolicyErrors sslPolicyError)
    {
        this.WriteEvent(EVENT_ID_SSL_POLICY_ERROR, sslPolicyError);
    }

    [Event(
        EVENT_ID_UNTRUSTED_CERT_ERROR,
        Message = "Server Certificates Chain cannot be trusted. The chain doesn't match with the Trusted Certificates provided. Server Certificates={0}. Trusted Certificates={1}.",
        Level = EventLevel.Warning)]
    public void UntrustedCertificateError(string serverCertificates, string trustedCertificates)
    {
        this.WriteEvent(EVENT_ID_UNTRUSTED_CERT_ERROR, serverCertificates, trustedCertificates);
    }
}
