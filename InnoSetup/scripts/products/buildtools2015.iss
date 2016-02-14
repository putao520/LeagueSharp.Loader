// requires Windows 10, Windows 7 Service Pack 1, Windows 8, Windows 8.1, Windows Server 2003 Service Pack 2, Windows Server 2008 R2 SP1, Windows Server 2008 Service Pack 2, Windows Server 2012, Windows Vista Service Pack 2, Windows XP Service Pack 3
// http://www.microsoft.com/en-US/download/details.aspx?id=48145

[CustomMessages]
buildtools2015_title=Microsoft Build Tools 2015

en.buildtools2015_size=24.5 MB
de.buildtools2015_size=24,5 MB

// {D1437F51-786A-4F57-A99C-F8E94FBA1BD8} Microsoft Build Tools 14.0 (x86) 14.0.23107

// {31FFFC1B-494E-4FF9-9D49-53ACCACB80FD} Microsoft Build Tools 14.0 (amd64)
// {118E863A-F6E9-4A5B-8C61-56B8B752A200} Microsoft Build Tools 14.0 (x86)

// {38368B5D-626E-41C1-A160-CB24B0BCE43C} Microsoft Build Tools 14.0 (amd64)
// {F86F966D-0332-4444-B4D0-FAE76B58D61F} Microsoft Build Tools 14.0 (x86)

// {2BDE4E1E-FE85-471C-8419-35CC61408E27} Microsoft Build Tools 14.0 (amd64)
// {477F7BAD-67AD-4E4F-B704-4AF4F44CB9BD} Microsoft Build Tools 14.0 (x86)


[Code]
const
	buildtools2015_url = 'http://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe';
	
var 
    missing: Boolean;

procedure buildtools2015();
begin
		missing := True;
		
		if msiproduct('{D1437F51-786A-4F57-A99C-F8E94FBA1BD8}') or 
		   msiproduct('{118E863A-F6E9-4A5B-8C61-56B8B752A200}') or 
		   msiproduct('{F86F966D-0332-4444-B4D0-FAE76B58D61F}') or 
		   msiproduct('{477F7BAD-67AD-4E4F-B704-4AF4F44CB9BD}') then 
		begin
			missing := False;
		end;

		if (missing) then
		begin
			AddProduct('BuildTools_Full.exe', '/passive /norestart',
				CustomMessage('buildtools2015_title'),
				CustomMessage('buildtools2015_size'),
				buildtools2015_url,
				false, false);
		end;
end;