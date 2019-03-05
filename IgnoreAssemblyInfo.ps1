$files = Get-ChildItem * -recurse | Where-Object {$_.Fullname.Contains("AssemblyInfo.vb")}
foreach($file in $files) {
	git update-index --assume-unchanged $file.fullname
}