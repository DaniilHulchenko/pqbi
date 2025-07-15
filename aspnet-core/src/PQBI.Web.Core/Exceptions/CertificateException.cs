using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PQBI.Web.Exceptions
{
    public class CertificateException : Exception
    {
        private readonly X509Certificate2 _certificate;

        public CertificateException(X509Certificate2 certificate)
        {
            this._certificate = certificate;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Effective date: {_certificate.GetEffectiveDateString()}");
            sb.AppendLine($"Exp date: {_certificate.GetExpirationDateString()}");
            sb.AppendLine($"Issuer: {_certificate.Issuer}");
            sb.AppendLine($"Subject: {_certificate.Subject}");


            return sb.ToString();
        }
    }
}