// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Security;

namespace OpenTelemetry.ResourceDetectors;

internal interface IServerCertificateValidationEventSource
{
    public void FailedToExtractResourceAttributes(string format, string exception);

    public void FailedToValidateCertificate(string error);

    public void InvalidCertificateError(string fileName);

    public void InvalidCertificateChainError(string error);

    public void SslPolicyError(SslPolicyErrors sslPolicyError);

    public void UntrustedCertificateError(string serverCertificates, string trustedCertificates);
}
