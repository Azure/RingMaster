# Download vcredist installers
Invoke-WebRequest "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe" -OutFile .\vcredistx64-11.exe
Invoke-WebRequest "https://download.microsoft.com/download/0/5/6/056dcda9-d667-4e27-8001-8a0c6971d6b1/vcredist_x64.exe" -OutFile .\vcredistx64-12.exe
Invoke-WebRequest "https://download.microsoft.com/download/9/3/F/93FCF1E7-E6A4-478B-96E7-D4B285925B00/vc_redist.x64.exe" -OutFile .\vcredistx64-14.exe

# VC11 (VS2012) VC12 (VS2013) VC14 (VS2015) runtime
& .\vcredistx64-11.exe /install /passive
& .\vcredistx64-12.exe /install /passive
& .\vcredistx64-14.exe /install /passive
