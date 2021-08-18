using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PdfSignSamplePkcs1
{
    public class SignService
    {
        System.Security.Cryptography.X509Certificates.X509Certificate2 signCertificatePrivateKey;

        public byte[] Digest { get; set; }
        public string PathToPrivateKey { get; set; }
        public string PrivatekeyPassword { get; set; }
        public SignService(byte[] digest, string pathToPrivateKey, string privatekeyPassword)
        {
            Digest = digest;
            PathToPrivateKey = pathToPrivateKey;
            PrivatekeyPassword = privatekeyPassword;
        }

        public byte[] CreatePKCS7()
        {
            //Load the certificate used for signing
            signCertificatePrivateKey = LoadCertificateFromFile();

            //Create signature >> Replace this code with a signature call to AIS
            //todo check which hash actually must be sent to the service (Might require the hash to be converted to SHA256)
            //todo maybe the certificate chain must be included in case of the AIS service
            Org.BouncyCastle.X509.X509Certificate cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(signCertificatePrivateKey);
            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)signCertificatePrivateKey.PrivateKey;
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetRsaKeyPair(rsa);
            PrivateKeySignature signature = new PrivateKeySignature(keyPair.Private, "SHA256");
            String hashAlgorithm = signature.GetHashAlgorithm();
            PdfPKCS7 sgn = new PdfPKCS7(null, new[] { cert }, hashAlgorithm, false);
            var sh = sgn.GetAuthenticatedAttributeBytes(Digest, PdfSigner.CryptoStandard.CMS, null, null);
            byte[] extSignature = signature.Sign(sh);

            //Apply signature
            sgn.SetExternalDigest(extSignature, null, signature.GetEncryptionAlgorithm());

            //Return the complete PKCS7 CMS 
            return sgn.GetEncodedPKCS7(Digest, PdfSigner.CryptoStandard.CMS, null, null, null);

        }


        public byte[] CreatePKCS7ViaPkcs1()
        {
            //Load the certificate used for signing
            signCertificatePrivateKey = LoadCertificateFromFile();

            // create sha256 message digest
            // This is from https://kb.itextpdf.com/home/it7kb/examples/how-to-use-a-digital-signing-service-dss-such-as-globalsign-with-itext-7
            // Not sure if this is required, but the created signature is invalid either way
            using (SHA256 sha256 = SHA256.Create())
            {
                Digest = sha256.ComputeHash(Digest);
            }

            //Create pkcs1 signature using RSA
            byte[] signature = null;
            using (var key = signCertificatePrivateKey.GetRSAPrivateKey())
            {
                signature = key.SignData(Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            Org.BouncyCastle.X509.X509Certificate cert = DotNetUtilities.FromX509Certificate(signCertificatePrivateKey);
            PdfPKCS7 sgn = new PdfPKCS7(null, new[] { cert }, "SHA256", false);
            sgn.SetExternalDigest(signature, null, "RSA");

            //Return the complete PKCS7 CMS 
            return sgn.GetEncodedPKCS7(Digest, PdfSigner.CryptoStandard.CMS, null, null, null);
        }


        public X509Certificate2 LoadCertificateFromFile()
        {
            X509Certificate2 rootCertificateWithPrivateKey = new X509Certificate2();
            byte[] rawData = System.IO.File.ReadAllBytes(PathToPrivateKey);
            rootCertificateWithPrivateKey.Import(rawData, PrivatekeyPassword, X509KeyStorageFlags.Exportable);
            return rootCertificateWithPrivateKey;
        }

    }
}
