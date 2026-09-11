# hidari-camera-on　動作確認用リクエスト

## 死活監視

```sh
curl --request GET --include --url "http://127.0.0.1:15082/health"
```


## コメントAPI

非発動：

```sh
curl --request POST --include --header 'Authorization: Bearer comment-test-token' --header 'Content-Type: application/json' --data '{"requestId": "11111111-1111-4111-8111-111111111111", "source": "boyomi-proxy", "eventType": "comment", "text": "こんにちは", "receivedAt": "2026-09-11T12:30:00+09:00", "sessionId": null}' --url "http://127.0.0.1:15082/api/v1/comments"
```

発動：

```sh
curl --request POST --include --header 'Authorization: Bearer comment-test-token' --header 'Content-Type: application/json' --data '{"requestId": "11111111-1111-4111-8111-111111111111", "source": "boyomi-proxy", "eventType": "comment", "text": "左カメラON", "receivedAt": "2026-09-11T12:30:00+09:00", "sessionId": null}' --url "http://127.0.0.1:15082/api/v1/comments"
```


## 音声認識API

非発動：

```sh
curl --request POST --include --header 'Authorization: Bearer speech-test-token' --header 'Content-Type: application/json' --data '{"requestId": "55555555-5555-4555-8555-555555555555", "source": "speech-recognition-telop", "eventType": "speech-recognition", "text": "はいみなさんこんにちは", "receivedAt": "2026-09-11T12:34:00+09:00", "sessionId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"}' --url "http://127.0.0.1:15082/api/v1/speech-recognition"
```

発動：

```sh
curl --request POST --include --header 'Authorization: Bearer speech-test-token' --header 'Content-Type: application/json' --data '{"requestId": "55555555-5555-4555-8555-555555555555", "source": "speech-recognition-telop", "eventType": "speech-recognition", "text": "左カメラオン", "receivedAt": "2026-09-11T12:34:00+09:00", "sessionId": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"}' --url "http://127.0.0.1:15082/api/v1/speech-recognition"
```
