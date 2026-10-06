import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateGoalPayload, GoalDto, UpdateGoalPayload } from './models';

@Injectable({ providedIn: 'root' })
export class GoalsService {
  private readonly http = inject(HttpClient);

  listGoals(): Observable<GoalDto[]> {
    return this.http.get<GoalDto[]>('/api/goals');
  }

  createGoal(payload: CreateGoalPayload): Observable<GoalDto> {
    return this.http.post<GoalDto>('/api/goals', payload);
  }

  updateGoal(id: number, payload: UpdateGoalPayload): Observable<GoalDto> {
    return this.http.put<GoalDto>(`/api/goals/${id}`, payload);
  }

  deleteGoal(id: number): Observable<void> {
    return this.http.delete<void>(`/api/goals/${id}`);
  }
}
