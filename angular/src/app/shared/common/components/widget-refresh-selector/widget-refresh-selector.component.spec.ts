import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WidgetRefreshSelectorComponent } from './widget-refresh-selector.component';

describe('WidgetRefreshSelectorComponent', () => {
  let component: WidgetRefreshSelectorComponent;
  let fixture: ComponentFixture<WidgetRefreshSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WidgetRefreshSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(WidgetRefreshSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
