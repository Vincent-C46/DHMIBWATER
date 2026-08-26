$ErrorActionPreference = 'Stop'

$sampleBase = 'F:\02_Work\05_Addin_Docs\05_관로_애드인관련\BIM 기반 관로 설계 애드인 개발\10_수령자료\260720_태열_shp샘플\(B010)수치지도_33610067_2022_00000686556044\N3L_F0010000'
$assemblyPath = Join-Path $PSScriptRoot '..\src\DHBIMWATER.Infrastructure\bin\Debug\net8.0-windows\DHBIMWATER.Infrastructure.dll'

$assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $assemblyPath))
$readerType = $assembly.GetType('DHBIMWATER.Infrastructure.Repositories.Gis.DbfTableReader', $true)
$readMethod = $readerType.GetMethod('Read', [System.Reflection.BindingFlags]'Public,NonPublic,Static')
$result = $readMethod.Invoke($null, @("$sampleBase.dbf", "$sampleBase.cpg"))

$fieldNames = @($result.Fields | ForEach-Object Name)
if (($fieldNames -join ',') -ne '구분,등고수치,UFID') {
    throw "필드명이 일치하지 않습니다: $($fieldNames -join ',')"
}
if ($result.EncodingName -ne 'ks_c_5601-1987') {
    throw "인코딩 WebName이 일치하지 않습니다: $($result.EncodingName)"
}
$first = $result.Rows[0]
if ($first['구분'] -ne '주곡선' -or $first['등고수치'] -ne '60.0000') {
    throw "첫 레코드가 일치하지 않습니다: 구분=$($first['구분']), 등고수치=$($first['등고수치'])"
}
if ($fieldNames | Where-Object { $_ -match '\?' }) {
    throw '필드명에 물음표가 남아 있습니다.'
}

$fallbackResult = $readMethod.Invoke($null, @("$sampleBase.dbf", "$sampleBase.missing.cpg"))
if ($fallbackResult.EncodingName -ne 'ks_c_5601-1987' -or (($fallbackResult.Fields | ForEach-Object Name) -join ',') -ne '구분,등고수치,UFID') {
    throw '.cpg 없는 CP949 폴백 검증에 실패했습니다.'
}

Write-Output 'DBF_KOREAN_FIELDS_VERIFIED'
