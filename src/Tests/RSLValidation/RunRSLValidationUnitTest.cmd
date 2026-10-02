rem "Cleanup self-signed certificates"
powershell -command "get-childitem cert:LocalMachine\My | where-object { $_.Subject.Contains('RSLValidationUnitTest.Certificate') } | Remove-Item"
powershell -command "get-childitem cert:LocalMachine\Root | where-object { $_.Subject.Contains('RSLValidationUnitTest.Certificate') } | Remove-Item"

if "%1" == "cleanup" goto :end

rem "Install self-signed certificates that will be used by the SecureTransport unit test"

# we need to do it through makecert, because we want a trust chain
makecert -n "CN=RSLValidationUnitTest.CertificateRoot" -ss Root -sr LocalMachine -a sha1 -eku 1.3.6.1.5.5.7.3.3 -r
makecert -pe -n "CN=RSLValidationUnitTest.Certificate1" -ss MY -sr LocalMachine -a sha1 -eku 1.3.6.1.5.5.7.3.3 -is Root -ir LocalMachine -in "RSLValidationUnitTest.CertificateRoot"
makecert -pe -n "CN=RSLValidationUnitTest.Certificate2" -ss MY -sr LocalMachine -a sha1 -eku 1.3.6.1.5.5.7.3.3 -is Root -ir LocalMachine -in "RSLValidationUnitTest.CertificateRoot"

makecert -n "CN=RSLValidationUnitTest.CertificateRoot2" -ss Root -sr LocalMachine -a sha1 -eku 1.3.6.1.5.5.7.3.3 -r
makecert -pe -n "CN=RSLValidationUnitTest.Certificate3" -ss MY -sr LocalMachine -a sha1 -eku 1.3.6.1.5.5.7.3.3 -is Root -ir LocalMachine -in "RSLValidationUnitTest.CertificateRoot2"

:end