import requests
import json

base_url = "http://localhost:5171"

def test_api():
    print("--- 1. Testing Unauthorized Access ---")
    r = requests.get(f"{base_url}/users")
    print(f"Status (Expected 401): {r.status_code}")

    print("\n--- 2. Testing Login ---")
    login_data = {"username": "admin", "password": "password"}
    r = requests.post(f"{base_url}/login", json=login_data)
    print(f"Login Status: {r.status_code}")
    
    if r.status_code != 200:
        print("Login failed, stopping tests.")
        return

    token = r.json().get("token")
    headers = {"Authorization": f"Bearer {token}"}
    print(f"Token acquired.")

    print("\n--- 3. Testing Authorized Access (GET /users) ---")
    r = requests.get(f"{base_url}/users", headers=headers, params={"page": 1, "pageSize": 2})
    print(f"Status: {r.status_code}")
    print(f"Body: {json.dumps(r.json(), indent=2)}")

    print("\n--- 4. Testing Standardized Validation Error (POST /users) ---")
    # Missing required fields
    r = requests.post(f"{base_url}/users", headers=headers, json={"name": ""})
    print(f"Status (Expected 400): {r.status_code}")
    print(f"Error Body: {json.dumps(r.json(), indent=2)}")

    print("\n--- 5. Testing Standardized 404 Error ---")
    r = requests.get(f"{base_url}/users/9999", headers=headers)
    print(f"Status (Expected 404): {r.status_code}")
    print(f"Error Body: {json.dumps(r.json(), indent=2)}")

    print("\n--- 6. Clean CRUD Verification ---")
    payload = {"name": "Phase 3 User", "email": "p3@techhive.com", "role": "Architect"}
    r = requests.post(f"{base_url}/users", headers=headers, json=payload)
    print(f"POST Status: {r.status_code}")
    uid = r.json().get("id")
    
    r = requests.delete(f"{base_url}/users/{uid}", headers=headers)
    print(f"DELETE Status: {r.status_code}")

if __name__ == "__main__":
    test_api()
