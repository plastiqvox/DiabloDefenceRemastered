# Assets

디아블로 1 그림·소리 자원이라 저장소에 올리지 않는다(이 파일만 추적한다). 클론한 뒤 한 번 가져오기로 채운다.

```
powershell -ExecutionPolicy Bypass -File tools\import-all.ps1 -Diabdat "<디아블로 폴더>\diabdat.mpq" -Exe "<원작 경로>\bin\Debug\DiabloDefence.exe"
```

- 뿌리의 `*.png`·`DIInop.cur` — 원작 DiabloDefence 실행 파일의 그림(`tools\import-assets.ps1`).
- `d1/` — `diabdat.mpq`(와 헬파이어 MPQ)에서 가져온 것(`tools\DiabloAssetImporter`). `d1/manifest.json` 이 마지막에 쓰이므로 없으면 가져오기가 끝나지 않은 것이다.
  `d1/import.log` 에 마지막 실행 보고, `d1/.cache/` 에 다시 가져오기용 캐시가 있다.
- 손으로 고치지 않는다. 섞였거나 낡았다고 나오면 가져오기를 다시 돌린다(`--force` 는 캐시를 무시).
- 옵션·출력 모양은 `docs/architecture.md` 1.4.
