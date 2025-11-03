import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateOrEditCardConfigurationComponent } from './create-or-edit-card-configuration.component';

describe('CreateOrEditCardConfigurationComponent', () => {
  let component: CreateOrEditCardConfigurationComponent;
  let fixture: ComponentFixture<CreateOrEditCardConfigurationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateOrEditCardConfigurationComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CreateOrEditCardConfigurationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
