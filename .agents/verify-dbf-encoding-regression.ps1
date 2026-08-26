$ErrorActionPreference = 'Stop'
[System.Text.Encoding]::RegisterProvider([System.Text.CodePagesEncodingProvider]::Instance)
$encoding = [System.Text.Encoding]::GetEncoding(949)

$english = [byte[]](0x55, 0x46, 0x49, 0x44, 0, 0xB1, 0xB8, 0xBA, 0xD0, 0, 0)
$englishLength = [Array]::IndexOf($english, [byte]0, 0, 11)
$englishName = $encoding.GetString($english, 0, $englishLength).TrimEnd(' ')
if ($englishName -ne 'UFID') {
    throw "영문 필드명 회귀: $englishName"
}

$korean = [byte[]](0xB1, 0xB8, 0xBA, 0xD0, 0, 0xB5, 0xEE, 0xB0, 0xED, 0, 0)
$koreanLength = [Array]::IndexOf($korean, [byte]0, 0, 11)
$koreanName = $encoding.GetString($korean, 0, $koreanLength).TrimEnd(' ')
if ($koreanName -ne '구분') {
    throw "NUL 경계 디코딩 회귀: $koreanName"
}

Write-Output 'DBF_ENCODING_REGRESSION_VERIFIED'
