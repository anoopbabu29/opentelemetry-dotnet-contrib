// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK

using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace OpenTelemetry.ResourceDetectors;

internal class ServerCertificateValidationProvider
{
    public static IServerCertificateValidationEventSource? Log;

    private readonly X509Certificate2Collection trustedCertificates;

    private ServerCertificateValidationProvider(X509Certificate2Collection trustedCertificates, IServerCertificateValidationEventSource? log = null)
    {
        this.trustedCertificates = trustedCertificates;
        this.ValidationCallback = (_, cert, chain, errors) =>
            this.ValidateCertificate(cert != null ? new X509Certificate2(cert) : null, chain, errors);
        Log = log;
    }

    public RemoteCertificateValidationCallback ValidationCallback { get; }

    public static ServerCertificateValidationProvider? FromCertificateFile(string certificateFile, IServerCertificateValidationEventSource? log = null)
    {
        if (!File.Exists(certificateFile))
        {
            log?.InvalidCertificateError(certificateFile);
            return null;
        }

        var trustedCertificates = new X509Certificate2Collection();
        if (!LoadCertificateToTrustedCollection(trustedCertificates, certificateFile))
        {
            log?.InvalidCertificateError(certificateFile);
            return null;
        }

        return new ServerCertificateValidationProvider(trustedCertificates, log);
    }

    private static bool LoadCertificateToTrustedCollection(X509Certificate2Collection collection, string certFileName)
    {
        try
        {
            collection.Import(certFileName);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool HasCommonCertificate(X509Chain chain, X509Certificate2Collection? collection)
    {
        if (collection == null)
        {
            return false;
        }

        foreach (var chainElement in chain.ChainElements)
        {
            foreach (var certificate in collection)
            {
                if (chainElement.Certificate.GetPublicKey().SequenceEqual(certificate.GetPublicKey()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ValidateCertificate(X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        var isSslPolicyPassed = errors == SslPolicyErrors.None ||
                                errors == SslPolicyErrors.RemoteCertificateChainErrors;
        if (!isSslPolicyPassed)
        {
            if ((errors | SslPolicyErrors.RemoteCertificateNotAvailable) == errors)
            {
                Log?.SslPolicyError(SslPolicyErrors.RemoteCertificateNotAvailable);
            }

            if ((errors | SslPolicyErrors.RemoteCertificateNameMismatch) == errors)
            {
                Log?.SslPolicyError(SslPolicyErrors.RemoteCertificateNameMismatch);
            }
        }

        if (chain == null)
        {
            Log?.InvalidCertificateChainError("Certificate Chain is null.");
            return false;
        }

        if (cert == null)
        {
            Log?.InvalidCertificateError("Certificate is null.");
            return false;
        }

        chain.ChainPolicy.ExtraStore.AddRange(this.trustedCertificates);
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

        // building the chain to process basic validations e.g. signature, use, expiration, revocation
        var isValidChain = chain.Build(cert);

        if (!isValidChain)
        {
            var chainErrors = string.Empty;
            foreach (var element in chain.ChainElements)
            {
                foreach (var status in element.ChainElementStatus)
                {
                    chainErrors +=
                        $"\nCertificate [{element.Certificate.Subject}] Status [{status.Status}]: {status.StatusInformation}";
                }
            }

            Log?.InvalidCertificateChainError(chainErrors);
        }

        // check if at least one certificate in the chain is in our trust list
        var isTrusted = HasCommonCertificate(chain, this.trustedCertificates);
        if (!isTrusted)
        {
            var serverCertificates = string.Empty;
            foreach (var element in chain.ChainElements)
            {
                serverCertificates += " " + element.Certificate.Subject;
            }

            var trustCertificates = string.Empty;
            foreach (var trustCertificate in this.trustedCertificates)
            {
                trustCertificates += " " + trustCertificate.Subject;
            }

            Log?.UntrustedCertificateError(serverCertificates, trustCertificates);
        }

        return isSslPolicyPassed && isValidChain && isTrusted;
    }
}
#endif
