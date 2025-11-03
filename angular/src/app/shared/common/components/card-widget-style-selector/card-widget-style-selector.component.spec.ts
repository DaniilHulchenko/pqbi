import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetStyleSelectorComponent } from './card-widget-style-selector.component';

describe('CardWidgetStyleSelectorComponent', () => {
  let component: CardWidgetStyleSelectorComponent;
  let fixture: ComponentFixture<CardWidgetStyleSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetStyleSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetStyleSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
