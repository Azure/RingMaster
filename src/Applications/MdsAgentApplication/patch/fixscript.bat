@echo off

echo ##############################
echo Checking if the December SSL Admin cert is already in disallowed store
certutil -verifystore disallowed 948e1652586240d453287ab69caeb8f2f4f02117
if %errorlevel%==0 (
	echo The December SSL Admin cert is already in disallowed store. Exiting the script.
	goto :eof
)

(
echo -----BEGIN CERTIFICATE-----
echo MIIF4TCCBMmgAwIBAgIEByeqRzANBgkqhkiG9w0BAQsFADBaMQswCQYDVQQGEwJJ
echo RTESMBAGA1UEChMJQmFsdGltb3JlMRMwEQYDVQQLEwpDeWJlclRydXN0MSIwIAYD
echo VQQDExlCYWx0aW1vcmUgQ3liZXJUcnVzdCBSb290MB4XDTE0MDUwNzE3MDQwOVoX
echo DTE4MDUwNzE3MDMzMFowgYsxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5n
echo dG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9y
echo YXRpb24xFTATBgNVBAsTDE1pY3Jvc29mdCBJVDEeMBwGA1UEAxMVTWljcm9zb2Z0
echo IElUIFNTTCBTSEEyMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA0eg3
echo p3aKcEsZ8CA3CSQ3f+r7eOYFumqtTicN/HJq2WwhxGQRlXMQClwle4hslAT9x9uu
echo e9xKCLM+FvHQrdswbdcaHlK1PfBHGQPifaa9VxM/VOo6o7F3/ELwY0lqkYAuMEnA
echo iusrr/466wddBvfp/YQOkb0JICnobl0JzhXT5+/bUOtE7xhXqwQdvDH593sqE8/R
echo PVGvG8W1e+ew/FO7mudj3kEztkckaV24Rqf/ravfT3p4JSchJjTKAm43UfDtWBpg
echo lPbEk9jdMCQl1xzrGZQ1XZOyrqopg3PEdFkFUmed2mdROQU6NuryHnYrFK7sPfkU
echo mYsHbrznDFberL6u23UykJ5jvXS/4ArK+DSWZ4TN0UI4eMeZtgzOtg/pG8v0Wb4R
echo DsssMsj6gylkeTyLS/AydGzzk7iWa11XWmjBzAx5ihne9UkCXgiAAYkMMs3S1pbV
echo S6Dz7L+r9H2zobl82k7X5besufIlXwHLjJaoKK7BM1r2PwiQ3Ov/OdgmyBKdHJqq
echo qcAWjobtZ1KWAH8Nkj092XA25epCbx+uleVbXfjQOsfU3neG0PyeTuLiuKloNwnE
echo OeOFuInzH263bR9KLxgJb95KAY8Uybem7qdjnzOkVHxCg2i4pd+/7LkaXRM72a1o
echo /SAKVZEhZPnXEwGgCF1ZiRtEr6SsxwUQ+kFKqPsCAwEAAaOCAXswggF3MBIGA1Ud
echo EwEB/wQIMAYBAf8CAQAwYAYDVR0gBFkwVzBIBgkrBgEEAbE+AQAwOzA5BggrBgEF
echo BQcCARYtaHR0cDovL2N5YmVydHJ1c3Qub21uaXJvb3QuY29tL3JlcG9zaXRvcnku
echo Y2ZtMAsGCSsGAQQBgjcqATBCBggrBgEFBQcBAQQ2MDQwMgYIKwYBBQUHMAGGJmh0
echo dHA6Ly9vY3NwLm9tbmlyb290LmNvbS9iYWx0aW1vcmVyb290MA4GA1UdDwEB/wQE
echo AwIBhjAnBgNVHSUEIDAeBggrBgEFBQcDAQYIKwYBBQUHAwIGCCsGAQUFBwMJMB8G
echo A1UdIwQYMBaAFOWdWTCCR1jMrPoIVDaGezq1BE3wMEIGA1UdHwQ7MDkwN6A1oDOG
echo MWh0dHA6Ly9jZHAxLnB1YmxpYy10cnVzdC5jb20vQ1JML09tbmlyb290MjAyNS5j
echo cmwwHQYDVR0OBBYEFFGvJCac9GgiV4AmKztGYhV7HsylMA0GCSqGSIb3DQEBCwUA
echo A4IBAQBpYvaEkQDEb4J7JOFCoqWLglynxUTL51J2Y9N2nnjiaTWxOLqwlsYfrHvG
echo smV3i32NrmS5pYwXylhlw62C9cWi9QETk8Z+ROXEYfoDtlbBcuHIKMVpIY+sbv1/
echo Q4M2uMDWoCj+GkW+/ZOMjaRkeR8U26GfIdzATnsXIhextjzTm+IKo36ZsMGs2PSG
echo 3zzafRScQMF80hhv8U8mRQmVlFza0Jj49EyClhDerDDLK675kuq/eQP8Hj+sCaQ/
echo Zf2RT5Ykp860TmqWKReuwKjfFyL0F+PcHDkGVhDq6rV0FzxO3X6RCqgLeAenMUQI
echo MasYhA8SnOfehCzpbZNFv6jBPzTc
echo -----END CERTIFICATE-----
)>may_ssladmin_ca.cer

(
echo -----BEGIN CERTIFICATE-----
echo MIIFhjCCBG6gAwIBAgIEByeaqTANBgkqhkiG9w0BAQsFADBaMQswCQYDVQQGEwJJ
echo RTESMBAGA1UEChMJQmFsdGltb3JlMRMwEQYDVQQLEwpDeWJlclRydXN0MSIwIAYD
echo VQQDExlCYWx0aW1vcmUgQ3liZXJUcnVzdCBSb290MB4XDTEzMTIxOTIwMDczMloX
echo DTE3MTIxOTIwMDY1NVowgYsxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5n
echo dG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9y
echo YXRpb24xFTATBgNVBAsTDE1pY3Jvc29mdCBJVDEeMBwGA1UEAxMVTWljcm9zb2Z0
echo IElUIFNTTCBTSEEyMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA0eg3
echo p3aKcEsZ8CA3CSQ3f+r7eOYFumqtTicN/HJq2WwhxGQRlXMQClwle4hslAT9x9uu
echo e9xKCLM+FvHQrdswbdcaHlK1PfBHGQPifaa9VxM/VOo6o7F3/ELwY0lqkYAuMEnA
echo iusrr/466wddBvfp/YQOkb0JICnobl0JzhXT5+/bUOtE7xhXqwQdvDH593sqE8/R
echo PVGvG8W1e+ew/FO7mudj3kEztkckaV24Rqf/ravfT3p4JSchJjTKAm43UfDtWBpg
echo lPbEk9jdMCQl1xzrGZQ1XZOyrqopg3PEdFkFUmed2mdROQU6NuryHnYrFK7sPfkU
echo mYsHbrznDFberL6u23UykJ5jvXS/4ArK+DSWZ4TN0UI4eMeZtgzOtg/pG8v0Wb4R
echo DsssMsj6gylkeTyLS/AydGzzk7iWa11XWmjBzAx5ihne9UkCXgiAAYkMMs3S1pbV
echo S6Dz7L+r9H2zobl82k7X5besufIlXwHLjJaoKK7BM1r2PwiQ3Ov/OdgmyBKdHJqq
echo qcAWjobtZ1KWAH8Nkj092XA25epCbx+uleVbXfjQOsfU3neG0PyeTuLiuKloNwnE
echo OeOFuInzH263bR9KLxgJb95KAY8Uybem7qdjnzOkVHxCg2i4pd+/7LkaXRM72a1o
echo /SAKVZEhZPnXEwGgCF1ZiRtEr6SsxwUQ+kFKqPsCAwEAAaOCASAwggEcMBIGA1Ud
echo EwEB/wQIMAYBAf8CAQAwUwYDVR0gBEwwSjBIBgkrBgEEAbE+AQAwOzA5BggrBgEF
echo BQcCARYtaHR0cDovL2N5YmVydHJ1c3Qub21uaXJvb3QuY29tL3JlcG9zaXRvcnku
echo Y2ZtMA4GA1UdDwEB/wQEAwIBhjAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUH
echo AwIwHwYDVR0jBBgwFoAU5Z1ZMIJHWMys+ghUNoZ7OrUETfAwQgYDVR0fBDswOTA3
echo oDWgM4YxaHR0cDovL2NkcDEucHVibGljLXRydXN0LmNvbS9DUkwvT21uaXJvb3Qy
echo MDI1LmNybDAdBgNVHQ4EFgQUUa8kJpz0aCJXgCYrO0ZiFXsezKUwDQYJKoZIhvcN
echo AQELBQADggEBAHaFxSMxH7Rz6qC8pe3fRUNqf2kgG4Cy+xzdqn+I0zFBNvf7+2ut
echo mIx4H50RZzrNS+yovJ0VGcQ7C6eTzuj8nVvoH8tWrnZDK8cTUXdBqGZMX6fR16p1
echo xRspTMn0baFeoYWTFsLLO6sUfUT92iUphir+YyDK0gvCNBW7r1t/iuCq7UWm6nnb
echo 2DVmVEPeNzPR5ODNV8pxsH3pFndk6FmXudUu0bSR2ndx80oPSNI0mWCVN6wfAc0Q
echo negqpSDHUJuzbEl4K1iSZIm4lTaoNKrwQdKVWiRUl01uBcSVrcR6ozn7eQaKm6ZP
echo 2SL6RE4288kPpjnngLJev7050UblVUfbvG4=
echo -----END CERTIFICATE-----
)>dec_ssladmin_ca.cer

echo.
echo.
echo ##############################
echo Installing may cert to machine CA store
certutil -addstore CA may_ssladmin_ca.cer

echo.
echo.
echo ##############################
echo Installing may cert to user CA store
certutil -user -addstore CA may_ssladmin_ca.cer

echo.
echo.
echo ##############################
echo Installing dec cert to machine Untrusted Certificates store
certutil -addstore Disallowed dec_ssladmin_ca.cer

echo.
echo.
echo ##############################
echo Installing dec cert to user Untrusted Certificates store
certutil -user -addstore Disallowed dec_ssladmin_ca.cer

echo.
echo.
echo ##############################
echo Deleting dec cert from machine CA store
certutil -delstore CA 948e1652586240d453287ab69caeb8f2f4f02117

echo.
echo.
echo ##############################
echo Deleting dec cert from user CA store
certutil -user -delstore CA 948e1652586240d453287ab69caeb8f2f4f02117

echo.
echo.
echo ##############################
echo Clearing cache
certutil -urlcache * delete
certutil -setreg chain\ChainCacheResyncFiletime @now

echo.
echo.
echo ##############################
echo Script completed