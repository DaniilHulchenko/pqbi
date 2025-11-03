import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetParameterAdvancedSettingsComponent } from './card-widget-parameter-advanced-settings.component';

describe('CardWidgetParameterAdvancedSettingsComponent', () => {
  let component: CardWidgetParameterAdvancedSettingsComponent;
  let fixture: ComponentFixture<CardWidgetParameterAdvancedSettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetParameterAdvancedSettingsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetParameterAdvancedSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
