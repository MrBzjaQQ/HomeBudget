import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateGoalPayload, GoalDto } from './models';

@Injectable({ providedIn: 'root' })
export class GoalsService {
  private readonly http = inject(HttpClient);

  listGoals(): Observable<GoalDto[]> {
    return this.http.get<GoalDto[]>('/api/goals');
  }

  createGoal(payload: CreateGoalPayload): Observable<GoalDto> {
    return this.http.post<GoalDto>('/api/goals', payload);
  }

  deleteGoal(id: number): Observable<void> {
    return this.http.delete<void>(`/api/goals/${id}`);
  }
}
