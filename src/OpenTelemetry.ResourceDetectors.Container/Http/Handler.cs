// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK

using System;
using System.Net.Http;

namespace OpenTelemetry.ResourceDetectors.Container.Http;

internal class Handler
{
    public static HttpClientHandler? Create(string certificateFile)
    {
        try
        {
            ServerCertificateValidationProvider? serverCertificateValidationProvider =
                ServerCertificateValidationProvider.FromCertificateFile(certificateFile);

            if (serverCertificateValidationProvider == null)
            {
                ContainerExtensionsEventSource.Log.FailedToValidateCertificate(nameof(Handler), "Failed to Load the certificate file into trusted collection");
                return null;
            }

            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback =
                (sender, x509Certificate2, x509Chain, sslPolicyErrors) =>
                    serverCertificateValidationProvider.ValidationCallback(sender, x509Certificate2, x509Chain, sslPolicyErrors);
            return clientHandler;
        }
        catch (Exception ex)
        {
            ContainerExtensionsEventSource.Log.ExtractResourceAttributesException($"{nameof(Handler)} : Failed to create HttpClientHandler", ex);
        }

        return null;
    }
}
#endif
