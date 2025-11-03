import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateOrEditGaugeConfigurationComponent } from './create-or-edit-gauge-configuration.component';

describe('CreateOrEditGaugeConfigurationComponent', () => {
  let component: CreateOrEditGaugeConfigurationComponent;
  let fixture: ComponentFixture<CreateOrEditGaugeConfigurationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateOrEditGaugeConfigurationComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CreateOrEditGaugeConfigurationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
