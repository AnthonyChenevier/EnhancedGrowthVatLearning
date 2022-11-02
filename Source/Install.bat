REM ################ Mod build and install script (c) Andreas Pardeike 2020 ################
REM MODIFIED BY Anthony Chenevier 2020-2022: modified for my personal preferences for solution directory format and install location.
REM Also updated for recent changes to 1.1 & harmony usage, and took out the extra xcopys to a standalone install of rimworld 
REM All credit goes to Andreas Pardeike. Original script at https://gist.github.com/pardeike/08ff826bf40ee60452f02d85e59f32ff
REM
REM Call this script from Visual Studio's Build Events post-build event command line box:
REM "$(ProjectDir)Install.bat" "C:\steam\steamapps\common\RimWorld" $(ConfigurationName) "$(ProjectDir)" "$(ProjectName)" "About Common v1.3" "LoadFolders.xml"
REM < 0 this script            >< 1 Rimworld install location   >< 2 Release/Debug ><3 location of solution><4 Mod name ><5 folders to copy > <6 files to copy >
REM Take note of the double quotations.
REM The project structure should look like this: 
REM Modname
REM +- .git
REM +- .vs
REM +- About
REM |	+- About.xml
REM |	+- Preview.png
REM |	+- PublishedFileId.txt
REM +- Assemblies                      <----- this is for RW1.0 + Harmony 1 (if supported)
REM |	+- 0Harmony.dll
REM |	+- 0Harmony.dll.mbd
REM |	+- 0Harmony.pdb
REM |	+- Modname.dll
REM |	+- Modname.dll.mbd
REM |	+- Modname.pdb
REM +- Common
REM |	+- Defs
REM |	+- Languages
REM |	|	+- English
REM |	|		+- Keyed
REM |	|			+- Keys.xml
REM |	+- Patches
REM |	+- Textures
REM +- Source
REM |	+- .vs
REM |	+- obj
REM |	|  +- Debug
REM |	|  +- Release
REM |	+- packages
REM |	|	+- Lib.Harmony.2.x.x
REM |	+- Properties
REM |	+- Modname.csproj
REM |	+- Modname.csproj.user
REM |	+- packages.config
REM |	+- Modname.sln
REM |	+- Install.bat                  <----- this script
REM +- v1.1
REM |	+- Assemblies                   <----- this is for RW1.1 + Harmony 2 (if supported). Other supported rimworld versions should follow the same convention (v1.2, v1.3, v1.4)
REM |		+- 0Harmony.dll				<----- the actual harmony dll does not have to be included for release
REM |		+- 0Harmony.pdb
REM |		+- Modname.dll
REM |		+- Modname.pdb
REM +- .gitattributes
REM +- .gitignore
REM +- LICENSE.txt
REM +- LoadFolders.xml
REM +- README.md
REM +- Modname.zip
REM
REM Finally, configure Visual Studio's Debug configuration with the rimworld exe as an external
REM program and set the working directory to the directory containing the exe.
REM
REM To debug, build the project (this script will install the mod to the Rimworld mods directory), then run "Debug" (F5) which
REM will start RimWorld in paused state. Finally, choose "Debug -> Attach Unity Debugger" and
REM select the rimworld instance
@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

ECHO %~1
ECHO %2
ECHO %~3
ECHO %~4
ECHO %~5
ECHO %~6
SET OUTPUT_DIR=%~1
SET BUILD_CONFIGURATION=%2
SET SOLUTION_DIR=%~3
SET MOD_NAME=%~4
SET FOLDERS_TO_COPY=%~5
SET FILES_TO_COPY=%~6

FOR %%G IN (%FOLDERS_TO_COPY%) DO (SET LAST_FOLDER=%%G)

SET MOD_DIR=%SOLUTION_DIR:~0,-8%

SET ZIP_FILE=%MOD_DIR%\%MOD_NAME%.zip

SET TARGET_DIR=%OUTPUT_DIR%\Mods\%MOD_NAME%

SET MOD_DLL_PATH=%MOD_DIR%\%LAST_FOLDER%\Assemblies

SET ZIP_EXE="C:\Program Files\7-Zip\7z.exe"



ECHO Running post-build script:
ECHO ==========================


IF EXIST "%OUTPUT_DIR%" (
	IF "%TARGET_DIR%" == "%SOLUTION_DIR%" (
		ECHO Solution and mod target directory match. Skipping copy operation.
	) ELSE (
		ECHO Copying to %TARGET_DIR%
		IF NOT EXIST "%TARGET_DIR%" (
			MKDIR "%TARGET_DIR%"
		) ELSE (
			ECHO WARNING-'%TARGET_DIR%' already exists. Old files will be automatically deleted.
			DEL /S /Q "%TARGET_DIR%"
		)

		FOR %%G IN (%FOLDERS_TO_COPY%) DO (
			SET FOLDER=%%G
			ECHO Copying folder '%MOD_DIR%\!FOLDER!' to '%TARGET_DIR%\!FOLDER!'
			XCOPY /I /Y /E "%MOD_DIR%\!FOLDER!" "%TARGET_DIR%\!FOLDER!" 1>NUL
		)
		FOR %%G IN (%FILES_TO_COPY%) DO (
			SET FILE=%%G
			ECHO Copying file '%MOD_DIR%\!FILE!' to '%TARGET_DIR%\!FILE!'
			XCOPY /Y "%MOD_DIR%\!FILE!" "%TARGET_DIR%\" 1>NUL
		)
	)
	ECHO Processing zip file...
	IF EXIST "%ZIP_FILE%" (
		ECHO Deleting old '%ZIP_FILE%'
		DEL "%ZIP_FILE%" 1>NUL
	)

	ECHO Adding mod files to '%ZIP_FILE%'
	FOR %%G IN (%FOLDERS_TO_COPY%) DO (
		SET FOLDER=%%G
		ECHO Copying folder '%MOD_DIR%\!FOLDER!' to '%ZIP_FILE%'
		%ZIP_EXE% a "%ZIP_FILE%" "%MOD_DIR%\!FOLDER!" 1>NUL
	)
	FOR %%G IN (%FILES_TO_COPY%) DO (
		SET FILE=%%G
		ECHO Copying file '%MOD_DIR%\!FILE!' to '%ZIP_FILE%'
		%ZIP_EXE% a "%ZIP_FILE%" "%MOD_DIR%\!FILE!" 1>NUL
	)
)
ECHO ==========================
ECHO post-build script complete