# PowerShell Script to Create Test Certificate for TaskDockr MSIX
# Run this script as Administrator

param(
    [string]$CertificateName = "TaskDockr Beta",
    [string]$Subject = "CN=TaskDockr",
    [string]$OutputPath = "TaskDockr-Test.pfx"
)

Write-Host "Creating test certificate for TaskDockr MSIX package..." -ForegroundColor Green

# Create self-signed certificate
try {
    $cert = New-SelfSignedCertificate \
        -Type Custom \
        -Subject $Subject \
        -KeyUsage DigitalSignature \
        -FriendlyName $CertificateName \
        -CertStoreLocation "Cert:\CurrentUser\My" \
        -KeyExportPolicy Exportable \
        -KeySpec Signature \
        -KeyLength 2048 \
        -HashAlgorithm SHA256 \
        -NotAfter (Get-Date).AddYears(1)
    
    Write-Host "Certificate created successfully!" -ForegroundColor Green
    Write-Host "Certificate Thumbprint: $($cert.Thumbprint)" -ForegroundColor Yellow
    
    # Export certificate to PFX file
    $certPassword = ConvertTo-SecureString -String "TaskDockrBeta123!" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $OutputPath -Password $certPassword
    
    Write-Host "Certificate exported to: $OutputPath" -ForegroundColor Green
    Write-Host "Password: TaskDockrBeta123!" -ForegroundColor Yellow
    
    # Install certificate to Trusted People store
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPeople", "CurrentUser")
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()
    
    Write-Host "Certificate installed to TrustedPeople store" -ForegroundColor Green
    
} catch {
    Write-Host "Error creating certificate: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Use this certificate to sign your MSIX package"
Write-Host "2. Install the certificate on test machines"
Write-Host "3. Enable developer mode or sideloading on test machines"
Write-Host "4. Install the signed MSIX package for testing"