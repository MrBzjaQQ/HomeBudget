import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { UserDto } from './models';

export interface CreateUserPayload {
  username: string;
  displayName: string;
  password: string;
}

export interface UpdateUserPayload {
  displayName?: string;
  password?: string;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  list(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>('/api/users');
  }

  create(payload: CreateUserPayload): Observable<UserDto> {
    return this.http.post<UserDto>('/api/users', payload);
  }

  update(id: number, payload: UpdateUserPayload): Observable<UserDto> {
    return this.http.put<UserDto>(`/api/users/${id}`, payload);
  }
}
