import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Page } from '@shared/service-proxies/service-proxies';

@Injectable({ providedIn: 'root' })
export class DashboardPagesService {
    private readonly pages$ = new BehaviorSubject<Page[]>([]);

    setPages(pages: Page[]): void {
        this.pages$.next(pages || []);
    }

    getPages(): Observable<Page[]> {
        return this.pages$.asObservable();
    }

    findPage(pageId: string | null | undefined): Page | undefined {
        if (!pageId) {
            return undefined;
        }

        return this.pages$.getValue().find((page) => page.id === pageId);
    }
}
