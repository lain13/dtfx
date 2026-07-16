# Changelog

이 프로젝트는 [Keep a Changelog](https://keepachangelog.com/) 형식을 따릅니다.

## Unreleased

### Added

- DTFX 제품명과 프로젝트 구조
- DB 없이 실행할 수 있는 Quick Start
- 결과 우선순위, 인수 파싱, XSD 및 예제를 검증하는 스모크 테스트
- GitHub Actions 빌드 및 검증 워크플로
- 기여 및 보안 정책 문서
- MIT License

### Changed

- R7001 프로젝트 이름을 `IF.Batch.DTFX`로 변경
- 외부 명령의 stdout/stderr를 동시에 수집하고 종료 코드를 실행 결과에 반영
- `maxreadrows` 설정을 CSV 읽기와 파일 기반 `ForEach`에 적용
- Git 저장소에 불필요한 Subversion/AnkhSVN 메타데이터 제거

### Fixed

- PostgreSQL Bulk Insert 요소의 잘못된 XSD 참조
- 샘플 Bulk Insert의 원본/대상 데이터소스 방향
- Error보다 Warning이 우선되던 결과 병합
- 디렉터리 생성 실패가 숨겨지던 동작
- PostgreSQL Executor의 잘못된 오류 요소명
- 외부 명령 출력 버퍼로 인한 교착 가능성
- 코드 주석과 오류 메시지의 오탈자
