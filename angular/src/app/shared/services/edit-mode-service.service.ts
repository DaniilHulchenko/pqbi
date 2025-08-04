import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class EditModeService {
  private editMode$ = new BehaviorSubject<boolean>(
    localStorage.getItem('app.dashboard.editMode') === 'true'
  );

  setEditMode(enabled: boolean) {
    this.editMode$.next(enabled);
    localStorage.setItem('app.dashboard.editMode', String(enabled));
  }

  getEditMode(): Observable<boolean> {
    return this.editMode$.asObservable();
  }

  getEditModeValue(): boolean {
    return this.editMode$.getValue();
  }
}
