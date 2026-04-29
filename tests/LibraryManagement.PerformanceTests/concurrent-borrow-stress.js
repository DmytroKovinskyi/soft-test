import http from 'k6/http';
import { check, sleep } from 'k6';

// Stress test: Concurrent borrowing of popular books
// Simulates 100 virtual users trying to borrow books simultaneously

export const options = {
    stages: [
        { duration: '10s', target: 50 },   // Ramp up to 50 users
        { duration: '20s', target: 100 },  // Ramp up to 100 users
        { duration: '30s', target: 100 },  // Stay at 100 users
        { duration: '10s', target: 0 },    // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'],  // 95% of requests < 1s
        // No error rate threshold - we expect 400s for business rule violations
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5126';

export function setup() {
    // Get available books and members for testing
    const booksRes = http.get(`${BASE_URL}/api/books?available=true`);
    const membersRes = http.get(`${BASE_URL}/api/members`);

    const books = JSON.parse(booksRes.body);
    const members = JSON.parse(membersRes.body);

    return {
        bookIds: books.slice(0, 50).map(b => b.id),
        memberIds: members.filter(m => m.isActive).slice(0, 100).map(m => m.id),
    };
}

export default function (data) {
    if (data.bookIds.length === 0 || data.memberIds.length === 0) {
        console.log('No books or members available for testing');
        return;
    }

    const bookId = data.bookIds[Math.floor(Math.random() * data.bookIds.length)];
    const memberId = data.memberIds[Math.floor(Math.random() * data.memberIds.length)];

    const payload = JSON.stringify({
        bookId: bookId,
        memberId: memberId,
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const response = http.post(`${BASE_URL}/api/loans`, payload, params);

    check(response, {
        'POST /api/loans status is 201 or 400': (r) => r.status === 201 || r.status === 400,
    });

    sleep(0.3);
}
