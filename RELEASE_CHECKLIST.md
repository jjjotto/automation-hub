# Release Checklist for Automation Hub

Use this checklist when creating a new release of Automation Hub.

## Pre-Build Checklist

- [ ] All code changes committed and pushed
- [ ] Version number updated in `installer.iss` (line 6: `#define MyAppVersion "x.x.x"`)
- [ ] CHANGELOG or release notes prepared (if applicable)
- [ ] All tests passing (if tests exist)
- [ ] No pending TODO items for this release

## Build Environment Setup

- [ ] Windows 10/11 machine available
- [ ] .NET 8.0 SDK installed
  - Verify: `dotnet --version` (should be 8.0.100 or higher)
- [ ] Inno Setup 6 installed (for installer method)
  - Download from: https://jrsoftware.org/isdl.php
- [ ] Git repository up to date (`git pull`)

## Building the Installer

### Method 1: Windows Installer (Setup.exe)

- [ ] Open PowerShell in repository root
- [ ] Run: `.\build-installer.ps1`
- [ ] Wait for build to complete (2-5 minutes)
- [ ] Verify no errors in output
- [ ] Check `installer-output\` directory created
- [ ] Verify file exists: `AutomationHub-Setup-x.x.x.exe`
- [ ] Check file size: ~80-120 MB (reasonable size)

### Method 2: Portable ZIP Package

- [ ] Open PowerShell in repository root
- [ ] Run: `.\build-portable.ps1`
- [ ] Wait for build to complete (2-5 minutes)
- [ ] Verify no errors in output
- [ ] Verify file exists: `AutomationHub-Portable.zip`
- [ ] Check file size: ~80-120 MB (reasonable size)

## Testing

### Test on Build Machine

- [ ] Run installer or extract ZIP
- [ ] Launch application successfully
- [ ] Verify main window displays
- [ ] Verify job list loads (if sample jobs present)
- [ ] Close application cleanly

### Test on Clean Windows Machine

- [ ] Copy installer/ZIP to test machine
- [ ] Test machine should NOT have:
  - [ ] Visual Studio
  - [ ] .NET 8 SDK (Desktop Runtime is OK)
- [ ] Run installer
  - [ ] If .NET missing: Verify prompt appears and directs to download
  - [ ] If .NET present: Verify installation completes
- [ ] Launch application from Start Menu (installer) or launcher script (ZIP)
- [ ] Verify application runs correctly
- [ ] Check job loading works
- [ ] Close and verify clean exit

### Test Uninstallation (Installer Only)

- [ ] Open Windows Settings → Apps → Installed apps
- [ ] Find "Automation Hub"
- [ ] Uninstall
- [ ] Verify uninstall completes without errors
- [ ] Verify application folder removed
- [ ] Verify Start Menu shortcuts removed
- [ ] Check if config/logs preserved (if desired) or removed

## Distribution

### Internal Lab Distribution

- [ ] Copy to shared drive: `Y:\temporary_files\JO\automation\releases\`
- [ ] Create folder for version: `v1.0.0\`
- [ ] Copy both installer and ZIP (if both created)
- [ ] Create/update README in releases folder with:
  - [ ] Version number
  - [ ] Release date
  - [ ] What's new
  - [ ] Known issues
  - [ ] Installation instructions

### GitHub Release (if using GitHub)

- [ ] Create new tag: `git tag -a v1.0.0 -m "Version 1.0.0"`
- [ ] Push tag: `git push origin v1.0.0`
- [ ] Create GitHub Release
  - [ ] Upload installer: `AutomationHub-Setup-1.0.0.exe`
  - [ ] Upload portable: `AutomationHub-Portable.zip`
  - [ ] Write release notes
  - [ ] Mention system requirements
  - [ ] Link to QUICKSTART.md

## Documentation Updates

- [ ] Update README.md with new version (if needed)
- [ ] Update CHANGELOG.md (if maintaining one)
- [ ] Update QUICKSTART.md (if changes affect user experience)
- [ ] Verify all documentation links work
- [ ] Check that screenshots are current (if UI changed)

## Communication

### Notify Users

- [ ] Send email to lab users with:
  - [ ] Download location
  - [ ] What's new
  - [ ] Installation instructions (link to QUICKSTART.md)
  - [ ] Support contact information

### Update Internal Wiki/Documentation

- [ ] Update lab wiki with new version info
- [ ] Update installation procedures if changed
- [ ] Note any breaking changes
- [ ] Update FAQ if new issues discovered

## Post-Release

- [ ] Monitor for user issues in first 24-48 hours
- [ ] Document any installation problems encountered
- [ ] Create hotfix if critical issues found
- [ ] Archive build artifacts for this release
- [ ] Update project board/issue tracker with "Released in vX.X.X" labels

## Troubleshooting Build Issues

### Common Problems

**"dotnet command not found"**
- Solution: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

**"Inno Setup not found"**
- Solution: Install from https://jrsoftware.org/isdl.php OR use portable build method

**Build succeeds but installer fails**
- Check `publish\win-x64\` directory exists and contains files
- Verify `installer.iss` paths are correct
- Try running ISCC.exe manually on `installer.iss`

**Installer too large (>150 MB)**
- This may indicate duplicate dependencies
- Clean and rebuild: Delete `publish\` folder and try again

**Application won't start after install**
- Test on machine with .NET 8 Desktop Runtime
- Check Windows Event Viewer for .NET Runtime errors
- Verify all DLLs are in the install directory

## Rollback Procedure

If critical issues found after release:

1. **Immediate**
   - [ ] Remove installer from download location
   - [ ] Post notice: "Version X.X.X temporarily unavailable"
   
2. **Within 24 hours**
   - [ ] Identify and fix critical issue
   - [ ] Increment patch version (1.0.0 → 1.0.1)
   - [ ] Rebuild and retest
   - [ ] Re-release with fix
   
3. **Communication**
   - [ ] Email users about issue
   - [ ] Provide workaround if available
   - [ ] Announce fix when available

## Notes

- Keep build logs for troubleshooting
- Archive each release installer for historical reference
- Document any manual steps needed during build
- Update this checklist if new steps are discovered

---

**Release Manager Signature:**

Date: _______________

Version: _______________

Approved by: _______________
