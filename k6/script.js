import http from "k6/http";
import { check } from "k6";

export const options = {
    insecureSkipTLSVerify: true,
};

export default function () {
    const response = http.get(`http://localhost:8080/${getRndInteger(0, __ENV.MAX_NUM_KEYS)}`);
    check(response, {
        "status is 200": (r) => r.status === 200,
    });
}

function getRndInteger(min, max) {
    return Math.floor(Math.random() * (max - min) ) + min;
}

// usage (from the repo root): k6 run --vus 10 --duration 30s -e MAX_NUM_KEYS=22 ./k6/script.js