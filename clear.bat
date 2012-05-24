cd bin
del /Q *.*
cd ..

cd src

rd /s /q _ReSharper.RecoveryStar

cd Common
del /Q /A:R /A:H *.suo
cd Common
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd CommonResources
del /Q /A:R /A:H *.db
cd ..

cd Core
del /Q /A:R /A:H *.suo
cd Core
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd CRC-64
del /Q /A:R /A:H *.suo
cd CRC-64
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd FileAnalyzer
del /Q /A:R /A:H *.suo
cd FileAnalyzer
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd FileBrowser
del /Q /A:R /A:H *.suo
cd FileBrowser
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd FileCodec
del /Q /A:R /A:H *.suo
cd FileCodec
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd FileNamer
del /Q /A:R /A:H *.suo
cd FileNamer
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd FileSplitter
del /Q /A:R /A:H *.suo
cd FileSplitter
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd MACTrackBarLib
del /Q /A:R /A:H *.suo
cd MACTrackBarLib
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd PinkieControls
del /Q /A:R /A:H *.suo
cd PinkieControls
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd RecoveryStarCORE
del /Q /A:R /A:H *.suo
cd RecoveryStarCORE
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd RecoveryStarRUS
rd /s /q bin
rd /s /q obj
cd ..

cd RecoveryStarUSA
rd /s /q bin
rd /s /q obj
cd ..

cd RS-RAID
del /Q /A:R /A:H *.suo
cd RS-RAID
rd /s /q bin
rd /s /q obj
cd ..
cd ..

cd RS-RAID_Test
del /Q /A:R /A:H *.suo
cd RS-RAID_Test
rd /s /q bin
rd /s /q obj
cd ..
cd ..

del /Q /A:R /A:H *.suo