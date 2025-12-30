# 보기 - 터미널 선택하여 PowerShell 창 열기


1. 새 Feature- 브랜치 생성
	1) git checkout dev - dev 브랜치로 이동
	2) git pull origin dev - 원격 저장소의 최신 dev 브랜치 가져오기 (origin 은 원격 저장소 이름)
	3) git checkout -b feature/test - 새로운 feature/test 브랜치 생성 및 이동 (-b는 브랜치 생성 옵션)
	
	=> 또는 git checkout -b feature/기능명 dev (dev 브랜치를 기준으로 새 브랜치 생성)


2. 작업 후 커밋 및 푸시
	1) git status - 변경된 파일 확인
	2) git add . - 모든 변경 파일 스테이징 (스테이징은 커밋할 파일을 준비하는 단계)
	3) git commit -m "Add new feature" - 변경 사항 커밋 (메시지는 작업 내용에 맞게 작성 - 커밋은 변경 사항을 저장하는 단계)
	4) git push origin feature/test - 원격 저장소에 feature/test 브랜치 푸시 (origin 은 원격 저장소 이름)
	
	=>  git push (이후 브랜치 이름 생략 시 현재 브랜치가 푸시됨)

3. Pull Request 생성
	1) GitHub 저장소로 이동
	2) dev 브랜치를 현재 브랜치로 설정
	3) Compare & pull request 클릭
	4) 반드시 base 브랜치는 dev 로 설정
	5) PR 제목 및 설명 작성하고 Create pull request 클릭

4. 관리자 승인 및 병합
	1) 관리자가 PR 검토 및 승인
	2) Merge pull request 클릭하여 dev 브랜치에 병합
	3) Confirm merge 클릭하여 병합 완료

	5. feature 작업 중 dev 브랜치 변경 사항 반영
	1) git checkout dev - dev 브랜치로 이동
	2) git pull origin dev - 원격 저장소의 최신 dev 브랜치 가져오기
	3) git checkout feature/test - 작업 중인 feature 브랜치로 이동
	4) git merge dev - dev 브랜치의 변경 사항을 현재 브랜치에 병합
	5) 충돌 해결 후 git add . 및 git commit -m "Merge dev into feature/test" - 병합 커밋

5. 브랜치 삭제 (병합 후)
	1) git checkout dev - dev 브랜치로 이동
	2) git pull origin dev - 원격 저장소의 최신 dev 브랜치 가져오기
	3) git branch -d feature/test - 로컬에서 feature/test 브랜치 삭제
	4) git push origin --delete feature/test - 원격 저장소에서 feature/test 브랜치 삭제