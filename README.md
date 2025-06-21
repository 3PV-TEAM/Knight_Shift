# 로그라이크 액션 RPG - [Knight Shift]
<img src="/logo.png" alt="Knight Shift 로고" width="400"/>

> 🎮 3인칭 시점의 로그라이크 액션 RPG

## 📌 개요

이 프로젝트는 3D 로그라이크 액션 RPG입니다.  
플레이어는 스테이지를 선택하며 진행하고, 전투, 휴식, 이벤트 스테이지를 거쳐 능력을 강화해 최종 보스를 처치하는 것을 목표로 합니다.



## 🧑‍💻 팀 정보

| 이름 | 역할 |
|------|------|
| [송신화](https://github.com/myth0629) | 프로젝트 총괄 / 시스템 설계 |
| [최민서](https://github.com/minseo0316) | 보스 설계 |
| [김주형](https://github.com/jumenmarch9) | 맵, UI 설계 |



## 🕹️ 주요 기능

| 기능 구분 | 내용 |
|----------|------|
| 🎮 전투 시스템 | 실시간 3인칭 전투, 락온 타겟팅, 콤보 및 회피 가능 |
| 🧭 스테이지 선택 | 노드 기반 맵에서 스테이지 타입(전투/휴식/이벤트) 선택 |
| 🗡️ 능력 카드 | 전투 보상으로 획득, 능력 강화 또는 신규 스킬 부여 |
| 🧪 로그라이크 요소 | 매 게임마다 다른 루트와 보상, 무기/능력 랜덤화 |
| 🛍️ 상점 시스템 | 휴식 스테이지에서 회복 및 업그레이드 구매 가능 |
| 💀 보스 시스템 | 중간 보스 및 최종 보스 전투 구현 |



## 🛠️ 사용 기술

- Unity 6 6000.0.32f1 LTS
- C#
- Rider / VS Code
- GitHub (버전 관리)
- Firebase (데이터 저장)



## 🎯 설계 키포인트

- **Cinemachine**을 활용한 부드러운 카메라 전환 및 락온 시스템
- **ScriptableObject**를 활용한 능력 카드 및 무기 정보 관리
- **Firebase** 기반의 데이터 연동 (예: 골드 저장, 랭킹 시스템)
- **포탈 이동 및 씬 전환** 시스템으로 각 스테이지 연결성

![아키텍쳐](https://github.com/user-attachments/assets/ec9b5704-1b69-404b-83d9-771420e34f13)



## ERD 설계

<img src="/ERD.png" alt="ERD" width="864"/>



## 📄 발표 자료

- [프로젝트 계획서 ppt](https://docs.google.com/presentation/d/1ptdi_2S-oS0YnX8sOJYa5h880ndGW0KM/edit?usp=share_link&ouid=105161346780980050188&rtpof=true&sd=true)
- [Knight Shift 발표자료 ppt](https://docs.google.com/presentation/d/1_1scfc_A026orfoh6rfKEbZTNfrbryuR/edit?usp=sharing&ouid=105161346780980050188&rtpof=true&sd=true)



## 🎬 게임 플레이 영상

[![Knight Shift Gameplay](https://img.youtube.com/vi/xx2BGLpsjh0/0.jpg)](https://youtu.be/xx2BGLpsjh0)

> 클릭하여 재생


