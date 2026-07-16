# DTFX에 기여하기

이슈와 Pull Request를 환영합니다. 변경은 가능한 한 작고 검증 가능하게 유지해 주세요.

## 개발 환경

1. Windows에 .NET Framework 4.6.2 Developer Pack과 Visual Studio Build Tools를 설치합니다.
2. `NuGet.exe restore IF.Batch.sln`으로 패키지를 복원합니다.
3. `MSBuild.exe IF.Batch.sln /p:Configuration=Release /p:Platform="Any CPU"`로 빌드합니다.
4. `tests\DTFX.SmokeTests\bin\Release\DTFX.SmokeTests.exe`를 실행합니다.

## 변경 원칙

- 기존 XML 요소와 설정 키의 호환성을 우선합니다.
- 새 XML 요소나 속성을 추가하면 `Application.xsd`와 예제를 함께 갱신합니다.
- 버그 수정에는 가능한 범위에서 스모크 테스트를 추가합니다.
- 실제 연결 문자열, 암호, 고객 데이터 또는 내부 경로를 커밋하지 않습니다.
- 공개 API나 종료 코드가 달라지는 변경은 Pull Request 설명에 명시합니다.

Pull Request에는 변경 이유, 검증한 명령, 영향을 받는 DB 또는 XML 요소를 적어 주세요.

