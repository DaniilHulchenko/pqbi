import { TestBed } from '@angular/core/testing';

import { DashboardPagesService } from './dashboard-pages.service';

describe('DashboardPagesService', () => {
  let service: DashboardPagesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DashboardPagesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
