#
#   eduVPN - End-user friendly VPN
#
#   Copyright: 2017, The Commons Conservancy eduVPN Programme
#   SPDX-License-Identifier: GPL-3.0+
#

!IF "$(LANG)" == "en-US"
SETUP_NAME_LOC=$(SETUP_NAME)
WIX_LOC_FILE=eduVPN.wxl
!ELSE
SETUP_NAME_LOC=$(SETUP_NAME).$(LANG)
WIX_LOC_FILE=eduVPN.$(LANG).wxl
!ENDIF

Clean ::
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).msi" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).msi"
	-if exist "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).mst" del /f /q "$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).mst"


######################################################################
# Building
######################################################################

"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).msi" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\VC150Redist.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduEd25519.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduJSON.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOAuth.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduOpenVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.dll.wixobj" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\eduVPN.Client.exe.wixobj"
	-if exist $@ del /f /q $@
	"$(WIX)bin\light.exe" $(WIX_LIGHT_FLAGS) "-cultures:$(LANG)" -loc "$(WIX_LOC_FILE)" -out "$(@:"=).tmp" $**
	move /y "$(@:"=).tmp" $@ > NUL


!IF "$(LANG)" == "en-US"
# The en-US localization serves as the base. Therefore, it does not require the diff MST.
!ELSE
"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).mst" : \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME).msi" \
	"$(OUTPUT_DIR)\$(CFG)\$(PLAT)\$(SETUP_NAME_LOC).msi"
	cscript.exe $(CSCRIPT_FLAGS) "bin\MSI.wsf" //Job:MakeMST $** $@
!ENDIF
