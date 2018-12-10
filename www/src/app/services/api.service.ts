import { Injectable } from '@angular/core';
import { User } from '../models/user';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) {}

  register(user: User) {
    const httpOptions = {
      responseType: 'text/plain' as 'json'
    };
    return this.http
      .post('https://frkk4zmytl.execute-api.eu-central-1.amazonaws.com/Prod/', user, httpOptions)
      .toPromise();
  }
}
