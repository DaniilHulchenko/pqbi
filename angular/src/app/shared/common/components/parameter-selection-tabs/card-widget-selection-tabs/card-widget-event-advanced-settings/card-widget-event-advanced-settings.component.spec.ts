import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetEventAdvancedSettingsComponent } from './card-widget-event-advanced-settings.component';

describe('CardWidgetEventAdvancedSettingsComponent', () => {
  let component: CardWidgetEventAdvancedSettingsComponent;
  let fixture: ComponentFixture<CardWidgetEventAdvancedSettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetEventAdvancedSettingsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetEventAdvancedSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
