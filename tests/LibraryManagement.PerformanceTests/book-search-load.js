import http from 'k6/http';
import { check, sleep } from 'k6';

// Load test: Book search
// Simulates 50 virtual users searching books for 2 minutes

export const options = {
    stages: [
        { duration: '15s', target: 25 },  // Ramp up to 25 users
        { duration: '30s', target: 50 },   // Ramp up to 50 users
        { duration: '1m', target: 50 },    // Stay at 50 users
        { duration: '15s', target: 0 },    // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'],   // 95% of requests < 500ms
        http_req_failed: ['rate<0.01'],     // Error rate < 1%
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5126';

const categories = ['Fiction', 'Science', 'History', 'Technology', 'Philosophy', 'Art', 'Medicine', 'Law', 'Education', 'Sports'];

export default function () {
    // Search all books
    const allBooksRes = http.get(`${BASE_URL}/api/books`);
    check(allBooksRes, {
        'GET /api/books status is 200': (r) => r.status === 200,
        'GET /api/books returns array': (r) => JSON.parse(r.body).length > 0,
    });

    // Search by category
    const category = categories[Math.floor(Math.random() * categories.length)];
    const categoryRes = http.get(`${BASE_URL}/api/books?category=${category}`);
    check(categoryRes, {
        'GET /api/books?category status is 200': (r) => r.status === 200,
    });

    // Search available books
    const availableRes = http.get(`${BASE_URL}/api/books?available=true`);
    check(availableRes, {
        'GET /api/books?available status is 200': (r) => r.status === 200,
    });

    sleep(0.5);
}
